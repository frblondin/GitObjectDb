using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Migration;
using GitObjectDb.Reflection;
using GitObjectDb.Services;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models.Compare
{
    /// <inheritdoc/>
    [ExcludeFromGuardForNull]
    public sealed class MetadataTreeMerge : IMetadataTreeMerge
    {
        readonly IServiceProvider _serviceProvider;
        readonly IRepositoryProvider _repositoryProvider;
        readonly IModelDataAccessorProvider _modelDataProvider;
        readonly MigrationScaffolderFactory _migrationScaffolderFactory;
        readonly RepositoryDescription _repositoryDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeMerge"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="container">The container.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="repository">The repository on which to apply the merge.</param>
        /// <param name="mergeCommitId">The commit to be merged.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <exception cref="ArgumentNullException">
        /// serviceProvider
        /// or
        /// repositoryDescription
        /// or
        /// commitId
        /// or
        /// branchName
        /// or
        /// merger
        /// </exception>
        [ActivatorUtilitiesConstructor]
        public MetadataTreeMerge(IServiceProvider serviceProvider, IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, IObjectRepository repository, ObjectId mergeCommitId, string branchName)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Container = container ?? throw new ArgumentNullException(nameof(container));
            _repositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            CommitId = repository.CommitId ?? throw new GitObjectDbException("Repository instance is not linked to any commit.");
            MergeCommitId = mergeCommitId ?? throw new ArgumentNullException(nameof(mergeCommitId));
            BranchName = branchName ?? throw new ArgumentNullException(nameof(branchName));

            _repositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();
            _modelDataProvider = serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
            _migrationScaffolderFactory = serviceProvider.GetRequiredService<MigrationScaffolderFactory>();

            Initialize();
        }

        /// <summary>
        /// Gets the container.
        /// </summary>
        public IObjectRepositoryContainer Container { get; }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        public IObjectRepository Repository { get; }

        /// <inheritdoc/>
        public ObjectId CommitId { get; }

        /// <inheritdoc/>
        public ObjectId MergeCommitId { get; private set; }

        /// <inheritdoc/>
        public string BranchName { get; }

        /// <inheritdoc/>
        public bool IsPartialMerge { get; private set; }

        /// <inheritdoc/>
        public Migrator RequiredMigrator { get; private set; }

        /// <inheritdoc/>
        public bool RequiresMergeCommit { get; private set; }

        /// <summary>
        /// Gets all impacted paths.
        /// </summary>
        internal IEnumerable<string> AllImpactedPaths =>
            ModifiedChunks.Select(c => c.Path)
            .Union(AddedObjects.Select(a => a.Path), StringComparer.OrdinalIgnoreCase)
            .Union(DeletedObjects.Select(d => d.Path), StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public IList<MetadataTreeMergeChunkChange> ModifiedChunks { get; } = new List<MetadataTreeMergeChunkChange>();

        /// <inheritdoc/>
        public IList<MetadataTreeMergeObjectAdd> AddedObjects { get; } = new List<MetadataTreeMergeObjectAdd>();

        /// <inheritdoc/>
        public IList<MetadataTreeMergeObjectDelete> DeletedObjects { get; } = new List<MetadataTreeMergeObjectDelete>();

        static JObject GetContent(Commit mergeBase, string path, string branchInfo)
        {
            var blob = mergeBase[path]?.Target as Blob;
            var content = blob?.GetContentText() ??
                throw new NotImplementedException($"Could not find node {path} in {branchInfo} tree.");
            return JsonConvert.DeserializeObject<JObject>(content);
        }

        static JToken TryGetToken(JObject headObject, KeyValuePair<string, JToken> kvp)
        {
            return headObject.TryGetValue(kvp.Key, StringComparison.OrdinalIgnoreCase, out var headValue) ?
                headValue :
                null;
        }

        void Initialize()
        {
            _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                EnsureHeadCommit(repository);

                var mergeCommit = repository.Lookup<Commit>(MergeCommitId);
                var headTip = repository.Head.Tip;
                var baseCommit = repository.ObjectDatabase.FindMergeBase(headTip, mergeCommit);
                RequiresMergeCommit = headTip.Id != baseCommit.Id;

                var migrationScaffolder = _migrationScaffolderFactory(Container, _repositoryDescription);
                var migrators = migrationScaffolder.Scaffold(baseCommit.Id, MergeCommitId, MigrationMode.Upgrade);

                mergeCommit = ResolveRequiredMigrator(repository, mergeCommit, migrators);

                ComputeMerge(repository, baseCommit, mergeCommit, headTip);
            });
        }

        Commit ResolveRequiredMigrator(IRepository repository, Commit branchTip, IImmutableList<Migrator> migrators)
        {
            RequiredMigrator = migrators.Count > 0 ? migrators[0] : null;
            if (RequiredMigrator != null && RequiredMigrator.CommitId != MergeCommitId)
            {
                IsPartialMerge = true;

                branchTip = repository.Lookup<Commit>(RequiredMigrator.CommitId);
                MergeCommitId = RequiredMigrator.CommitId;
            }

            return branchTip;
        }

        /// <summary>
        /// Ensures that the head tip refers to the right commit.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <exception cref="GitObjectDbException">The current head commit id is different from the commit used by current repository.</exception>
        internal void EnsureHeadCommit(IRepository repository)
        {
            if (!repository.Head.Tip.Id.Equals(CommitId))
            {
                throw new GitObjectDbException("The current head commit id is different from the commit used by current repository.");
            }
        }

        void ComputeMerge(IRepository repository, Commit mergeBase, Commit branchTip, Commit headTip)
        {
            using (var branchChanges = repository.Diff.Compare<Patch>(mergeBase.Tree, branchTip.Tree))
            {
                using (var headChanges = repository.Diff.Compare<Patch>(mergeBase.Tree, headTip.Tree))
                {
                    foreach (var change in branchChanges)
                    {
                        switch (change.Status)
                        {
                            case ChangeKind.Modified:
                                ComputeMerge_Modified(mergeBase, branchTip, headTip, headChanges, change);
                                break;
                            case ChangeKind.Added:
                                ComputeMerge_Added(branchTip, change, headChanges);
                                break;
                            case ChangeKind.Deleted:
                                ComputeMerge_Deleted(mergeBase, change, headChanges);
                                break;
                            default:
                                throw new NotImplementedException($"Change type '{change.Status}' for branch merge is not supported.");
                        }
                    }
                }
            }
        }

        void ComputeMerge_Modified(Commit mergeBase, Commit branchTip, Commit headTip, Patch headChanges, PatchEntryChanges change)
        {
            var mergeBaseObject = GetContent(mergeBase, change.Path, "merge base");
            var branchObject = GetContent(branchTip, change.Path, "branch tip");
            var headObject = GetContent(headTip, change.Path, "head tip");

            AddModifiedChunks(change, mergeBaseObject, branchObject, headObject, headChanges[change.Path]);
        }

        void ComputeMerge_Added(Commit branchTip, PatchEntryChanges change, Patch headChanges)
        {
            var parentDataPath = change.Path.GetDataParentDataPath();
            if (headChanges.Any(c => c.Path.Equals(parentDataPath, StringComparison.OrdinalIgnoreCase) && c.Status == ChangeKind.Deleted))
            {
                throw new NotImplementedException("Node addition while parent has been deleted in head is not supported.");
            }

            var branchObject = GetContent(branchTip, change.Path, "branch tip");
            AddedObjects.Add(new MetadataTreeMergeObjectAdd(change.Path, branchObject));
        }

        void ComputeMerge_Deleted(Commit mergeBase, PatchEntryChanges change, Patch headChanges)
        {
            var folder = change.Path.Replace($"/{FileSystemStorage.DataFile}", string.Empty);
            if (headChanges.Any(c => c.Path.Equals(folder, StringComparison.OrdinalIgnoreCase) && (c.Status == ChangeKind.Added || c.Status == ChangeKind.Modified)))
            {
                throw new NotImplementedException("Node deletion while children have been added or modified in head is not supported.");
            }

            var mergeBaseObject = GetContent(mergeBase, change.Path, "branch tip");
            DeletedObjects.Add(new MetadataTreeMergeObjectDelete(change.Path, mergeBaseObject));
        }

        void AddModifiedChunks(PatchEntryChanges branchChange, JObject mergeBaseObject, JObject newObject, JObject headObject, PatchEntryChanges headChange)
        {
            if (headChange?.Status == ChangeKind.Deleted)
            {
                throw new NotImplementedException($"Conflict as a modified node {branchChange.Path} in merge branch source has been deleted in head.");
            }
            var type = Type.GetType(mergeBaseObject.Value<string>("$type"));
            var properties = _modelDataProvider.Get(type).ModifiableProperties;

            var changes = from kvp in (IEnumerable<KeyValuePair<string, JToken>>)newObject
                          let p = properties.TryGetWithValue(pr => pr.Name, kvp.Key)
                          where p != null
                          let mergeBaseValue = mergeBaseObject[kvp.Key]
                          where mergeBaseValue == null || !JToken.DeepEquals(kvp.Value, mergeBaseValue)
                          let headValue = TryGetToken(headObject, kvp)
                          select new MetadataTreeMergeChunkChange(branchChange.Path, mergeBaseObject, newObject, headObject, p, mergeBaseValue, kvp.Value, headValue);

            foreach (var modifiedProperty in changes)
            {
                ModifiedChunks.Add(modifiedProperty);
            }
        }

        /// <inheritdoc/>
        public ObjectId Apply(Signature merger) => new MetadataTreeMergeProcessor(_serviceProvider, _repositoryDescription, this).Apply(merger);
    }
}
