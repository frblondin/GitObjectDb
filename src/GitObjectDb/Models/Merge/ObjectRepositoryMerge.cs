using GitObjectDb.Attributes;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Migration;
using GitObjectDb.Reflection;
using GitObjectDb.Serialization;
using GitObjectDb.Services;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models.Merge
{
    /// <inheritdoc/>
    [ExcludeFromGuardForNull]
    internal sealed class ObjectRepositoryMerge : IObjectRepositoryMerge
    {
        private readonly MigrationScaffolderFactory _migrationScaffolderFactory;
        private readonly MergeProcessor.Factory _mergeProcessorFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryMerge"/> class.
        /// </summary>
        /// <param name="repository">The repository on which to apply the merge.</param>
        /// <param name="mergeCommitId">The commit to be merged.</param>
        /// <param name="branchName">Name of the branch.</param>
        /// <param name="migrationScaffolderFactory">The <see cref="MigrationScaffolder"/> factory.</param>
        /// <param name="mergeProcessorFactory">The <see cref="MergeProcessor"/> factory.</param>
        /// <param name="serializerFactory">The <see cref="ObjectRepositorySerializerFactory"/> factory.</param>
        [ActivatorUtilitiesConstructor]
        public ObjectRepositoryMerge(IObjectRepository repository, ObjectId mergeCommitId, string branchName,
            MigrationScaffolderFactory migrationScaffolderFactory, MergeProcessor.Factory mergeProcessorFactory,
            ObjectRepositorySerializerFactory serializerFactory)
        {
            if (serializerFactory == null)
            {
                throw new ArgumentNullException(nameof(serializerFactory));
            }

            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            HeadCommitId = repository.CommitId ?? throw new GitObjectDbException("Repository instance is not linked to any commit.");
            MergeCommitId = mergeCommitId ?? throw new ArgumentNullException(nameof(mergeCommitId));
            BranchName = branchName ?? throw new ArgumentNullException(nameof(branchName));

            _migrationScaffolderFactory = migrationScaffolderFactory ?? throw new ArgumentNullException(nameof(migrationScaffolderFactory));
            _mergeProcessorFactory = mergeProcessorFactory ?? throw new ArgumentNullException(nameof(mergeProcessorFactory));
            Serializer = serializerFactory(new ModelObjectSerializationContext(Repository.Container));

            Initialize();
        }

        public IObjectRepositorySerializer Serializer { get; }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        public IObjectRepository Repository { get; }

        /// <inheritdoc/>
        public ObjectId HeadCommitId { get; }

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

        /// <inheritdoc/>
        public IList<ObjectRepositoryPropertyChange> ModifiedProperties { get; } = new List<ObjectRepositoryPropertyChange>();

        /// <inheritdoc/>
        public IList<ObjectRepositoryAdd> AddedObjects { get; } = new List<ObjectRepositoryAdd>();

        /// <inheritdoc/>
        public IList<ObjectRepositoryDelete> DeletedObjects { get; } = new List<ObjectRepositoryDelete>();

        private IModelObject GetContent(Commit mergeBase, string path, string branchInfo)
        {
            var blob = mergeBase[path]?.Target as Blob;
            if (blob == null)
            {
                throw new NotImplementedException($"Could not find node {path} in {branchInfo} tree.");
            }
            return Serializer.Deserialize(blob.GetContentStream(),
                relativePath => (mergeBase[path.GetSiblingFile(relativePath)]?.Target as Blob)?.GetContentText() ?? string.Empty);
        }

        private void Initialize()
        {
            Repository.Execute(repository =>
            {
                EnsureHeadCommit(repository);

                var mergeCommit = repository.Lookup<Commit>(MergeCommitId);
                var headTip = repository.Head.Tip;
                var baseCommit = repository.ObjectDatabase.FindMergeBase(headTip, mergeCommit);
                RequiresMergeCommit = headTip.Id != baseCommit.Id;

                var migrationScaffolder = _migrationScaffolderFactory(Repository.Container, Repository.RepositoryDescription);
                var migrators = migrationScaffolder.Scaffold(baseCommit.Id, MergeCommitId, MigrationMode.Upgrade);

                mergeCommit = ResolveRequiredMigrator(repository, mergeCommit, migrators);

                ComputeMerge(repository, baseCommit, mergeCommit, headTip);
            });
        }

        private Commit ResolveRequiredMigrator(IRepository repository, Commit branchTip, IImmutableList<Migrator> migrators)
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
            if (!repository.Head.Tip.Id.Equals(HeadCommitId))
            {
                throw new GitObjectDbException("The current head commit id is different from the commit used by current repository.");
            }
        }

        private void ComputeMerge(IRepository repository, Commit mergeBase, Commit branchTip, Commit headTip)
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

        private void ComputeMerge_Modified(Commit mergeBase, Commit branchTip, Commit headTip, Patch headChanges, PatchEntryChanges change)
        {
            // Get data file path, in the case where a blob has changed
            var path = change.Path.GetSiblingFile(FileSystemStorage.DataFile);

            var mergeBaseObject = GetContent(mergeBase, path, "merge base");
            var branchObject = GetContent(branchTip, path, "branch tip");
            var headObject = GetContent(headTip, path, "head tip");

            AddModifiedProperties(change, mergeBaseObject, branchObject, headObject, headChanges[change.Path]);
        }

        private void ComputeMerge_Added(Commit branchTip, PatchEntryChanges change, Patch headChanges)
        {
            // Only data file changes have to be taken into account
            // Changes made to the blobs will product a 'modified' change as well
            if (Path.GetFileName(change.Path) != FileSystemStorage.DataFile)
            {
                return;
            }

            var parentDataPath = change.Path.GetDataParentDataPath();
            if (headChanges.Any(c => c.Path.Equals(parentDataPath, StringComparison.OrdinalIgnoreCase) && c.Status == ChangeKind.Deleted))
            {
                throw new NotImplementedException("Node addition while parent has been deleted in head is not supported.");
            }

            var branchObject = GetContent(branchTip, change.Path, "branch tip");
            var parentId = change.Path.GetDataParentId(Repository);
            AddedObjects.Add(new ObjectRepositoryAdd(change.Path, branchObject, parentId));
        }

        private void ComputeMerge_Deleted(Commit mergeBase, PatchEntryChanges change, Patch headChanges)
        {
            // Only data file changes have to be taken into account
            // Changes made to the blobs will product a 'modified' change as well
            if (Path.GetFileName(change.Path) != FileSystemStorage.DataFile)
            {
                return;
            }

            var folder = change.Path.Replace($"/{FileSystemStorage.DataFile}", string.Empty);
            if (headChanges.Any(c => c.Path.Equals(folder, StringComparison.OrdinalIgnoreCase) && (c.Status == ChangeKind.Added || c.Status == ChangeKind.Modified)))
            {
                throw new NotImplementedException("Node deletion while children have been added or modified in head is not supported.");
            }

            var mergeBaseObject = GetContent(mergeBase, change.Path, "branch tip");
            DeletedObjects.Add(new ObjectRepositoryDelete(change.Path, mergeBaseObject.Id));
        }

        private void AddModifiedProperties(PatchEntryChanges branchChange, IModelObject mergeBaseObject, IModelObject newObject, IModelObject headObject, PatchEntryChanges headChange)
        {
            if (headChange?.Status == ChangeKind.Deleted)
            {
                throw new NotImplementedException($"Conflict as a modified node {branchChange.Path} in merge branch source has been deleted in head.");
            }
            var changes = ComputeModifiedProperties(branchChange, mergeBaseObject, newObject, headObject);

            foreach (var modifiedProperty in changes)
            {
                if (typeof(IObjectRepositoryIndex).IsAssignableFrom(modifiedProperty.Property.Property.ReflectedType))
                {
                    // Indexes will be recomputed anyways from the changes when committed,
                    // so there is no need to track them in the modified chunks
                    continue;
                }

                ModifiedProperties.Add(modifiedProperty);
            }
        }

        internal static IEnumerable<ObjectRepositoryPropertyChange> ComputeModifiedProperties(PatchEntryChanges changes, IModelObject ancestor, IModelObject theirs, IModelObject ours)
        {
            return from property in ours.DataAccessor.ModifiableProperties
                   let ancestorChunk = GetChunk(ancestor, property)
                   let theirChunk = GetChunk(theirs, property)
                   where !ancestorChunk.HasSameValue(theirChunk)
                   let ourChunk = GetChunk(ours, property)
                   select new ObjectRepositoryPropertyChange(changes.Path, property, ancestorChunk, theirChunk, ourChunk);

            ObjectRepositoryPropertyValue GetChunk(IModelObject @object, ModifiablePropertyInfo property)
            {
                return new ObjectRepositoryPropertyValue(@object, property, property.Accessor(@object));
            }
        }

        /// <inheritdoc/>
        public ObjectId Apply(Signature merger) => _mergeProcessorFactory(this).Apply(merger);
    }
}
