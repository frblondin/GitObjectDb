using DiffPatch;
using DiffPatch.Data;
using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Compare
{
    /// <inheritdoc/>
    [ExcludeFromGuardForNull]
    public sealed class MetadataTreeMerge : IMetadataTreeMerge
    {
        readonly IRepositoryProvider _repositoryProvider;
        readonly IModelDataAccessorProvider _modelDataProvider;
        readonly Func<RepositoryDescription, IComputeTreeChanges> _computeTreeChangesFactory;
        readonly RepositoryDescription _repositoryDescription;
        readonly StringBuilder _jsonBuffer = new StringBuilder();

        readonly TreeDefinition _treeDefinition;
        ObjectId _branchTip;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeMerge"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit identifier.</param>
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
        public MetadataTreeMerge(IServiceProvider serviceProvider, RepositoryDescription repositoryDescription, ObjectId commitId, string branchName)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }
            _repositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));
            CommitId = commitId ?? throw new ArgumentNullException(nameof(commitId));
            BranchName = branchName ?? throw new ArgumentNullException(nameof(branchName));

            _repositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();
            _modelDataProvider = serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
            _computeTreeChangesFactory = serviceProvider.GetRequiredService<Func<RepositoryDescription, IComputeTreeChanges>>();

            _treeDefinition = Initialize();
        }

        /// <inheritdoc/>
        public ObjectId CommitId { get; }

        /// <inheritdoc/>
        public string BranchName { get; }

        static void CheckForConflict(MetadataTreeEntryChanges changes, FileDiff fileDiff, MetadataTreeChanges headChanges)
        {
            foreach (var matching in headChanges.Where(c => c.Path == changes.Path))
            {
                if (matching.Status == ChangeKind.Deleted)
                {
                    throw new NotSupportedException($"Node {matching.Path} has been modified in branch and deleted in head.");
                }
                var matchingPatch = DiffParserHelper.Parse(matching.Patch).Single();
                foreach (var chunk in fileDiff.Chunks)
                {
                    if (matchingPatch.Chunks.Any(c => AreChunksOverlapping(chunk, c)))
                    {
                        throw new NotSupportedException($"Node {matching.Path} has conflicting change chunks.");
                    }
                }
            }
        }

        static bool AreChunksOverlapping(Chunk chunkA, Chunk chunkB)
        {
            var modifiedLinesA = from c in chunkA.Changes where !c.Normal select c.Index;
            var modifiedLinesB = from c in chunkB.Changes where !c.Normal select c.Index;
            return modifiedLinesA.Intersect(modifiedLinesB).Any();
        }

        TreeDefinition Initialize()
        {
            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                EnsureHeadCommit(repository);

                var branch = repository.Branches[BranchName];
                var branchTip = branch.Tip;
                _branchTip = branchTip.Id;
                var headTip = repository.Head.Tip;
                var baseCommit = repository.ObjectDatabase.FindMergeBase(headTip, branchTip);

                var computeChanges = _computeTreeChangesFactory(_repositoryDescription);
                var branchDiff = computeChanges.Compare(GetType(), baseCommit.Id, branchTip.Id);
                var headDiff = computeChanges.Compare(GetType(), baseCommit.Id, headTip.Id);
                return ComputeMerge(repository, branchDiff, headDiff);
            });
        }

        void EnsureHeadCommit(IRepository repository)
        {
            if (!repository.Head.Tip.Id.Equals(CommitId))
            {
                throw new NotSupportedException("The current head commit id is different from the commit used by current instance.");
            }
        }

        TreeDefinition ComputeMerge(IRepository repository, MetadataTreeChanges branchChanges, MetadataTreeChanges headChanges)
        {
            var headTip = repository.Head.Tip;
            var definition = TreeDefinition.From(headTip);
            foreach (var change in branchChanges)
            {
                switch (change.Status)
                {
                    case ChangeKind.Added:
                        if (headChanges.Added.Any(n => n.New.Id == change.New.Id))
                        {
                            throw new NotSupportedException($"Same node with id {change.New.Id} added in both branches.");
                        }
                        var path = change.New.ToDataPath(_modelDataProvider);
                        change.New.ToJson(_jsonBuffer);
                        definition.Add(path, repository.CreateBlob(_jsonBuffer), Mode.NonExecutableFile);
                        break;
                    case ChangeKind.Modified:
                        var patch = DiffParserHelper.Parse(change.Patch).Single();

                        CheckForConflict(change, patch, headChanges);

                        var content = headTip[change.Path].Target.Peel<Blob>().GetContentText();
                        var modified = PatchHelper.Patch(content, patch.Chunks, "\n");
                        definition.Add(change.Path, repository.CreateBlob(modified), Mode.NonExecutableFile);
                        break;
                    case ChangeKind.Deleted:
                    default:
                        throw new NotImplementedException("Deletion for branch merge is not supported.");
                }
            }
            return definition;
        }

        /// <inheritdoc/>
        public Commit Apply(Signature merger)
        {
            if (merger == null)
            {
                throw new ArgumentNullException(nameof(merger));
            }

            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                EnsureHeadCommit(repository);
                var branch = repository.Branches[BranchName];
                var branchTip = branch.Tip;
                if (branchTip.Id != _branchTip)
                {
                    throw new NotImplementedException($"The branch {branch.FriendlyName} has changed since merge started.");
                }
                var message = $"Merge branch {branch.FriendlyName} into {repository.Head.FriendlyName}";
                return repository.Commit(_treeDefinition, message, merger, merger, mergeParent: branchTip);
            });
        }
    }
}
