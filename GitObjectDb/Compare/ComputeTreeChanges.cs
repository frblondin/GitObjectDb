using DiffPatch;
using DiffPatch.Data;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitObjectDb.Compare
{
    /// <inheritdoc/>
    internal class ComputeTreeChanges : IComputeTreeChanges
    {
        readonly IModelDataAccessorProvider _modelDataProvider;
        readonly IInstanceLoader _instanceLoader;
        readonly IRepositoryProvider _repositoryProvider;
        readonly RepositoryDescription _repositoryDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeTreeChanges"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <exception cref="ArgumentNullException">serviceProvider</exception>
        public ComputeTreeChanges(IServiceProvider serviceProvider, RepositoryDescription repositoryDescription)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _modelDataProvider = serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
            _instanceLoader = serviceProvider.GetRequiredService<IInstanceLoader>();
            _repositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();

            _repositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));
        }

        static void UpdateNodeIfNeeded(IMetadataObject original, IMetadataObject @new, Stack<string> stack, IModelDataAccessor accessor, IList<MetadataTreeEntryChanges> changes)
        {
            if (accessor.ModifiableProperties.Any(p => !p.AreSame(original, @new)))
            {
                var path = stack.ToDataPath();
                changes.Add(new MetadataTreeEntryChanges(path, ChangeKind.Modified, original, @new));
            }
        }

        void RemoveNode(IMetadataObject left, IList<MetadataTreeEntryChanges> changed, Stack<string> stack)
        {
            var path = stack.ToDataPath();
            changed.Add(new MetadataTreeEntryChanges(path, ChangeKind.Deleted, old: left));
            var dataAccessor = _modelDataProvider.Get(left.GetType());
            foreach (var childProperty in dataAccessor.ChildProperties)
            {
                stack.Push(childProperty.Name);
                foreach (var child in left.Children)
                {
                    stack.Push(child.Id.ToString());
                    RemoveNode(child, changed, stack);
                    stack.Pop();
                }
                stack.Pop();
            }
        }

        /// <inheritdoc/>
        public MetadataTreeChanges Compare(Type instanceType, ObjectId oldCommitId, ObjectId newCommitId)
        {
            if (oldCommitId == null)
            {
                throw new ArgumentNullException(nameof(oldCommitId));
            }
            if (newCommitId == null)
            {
                throw new ArgumentNullException(nameof(newCommitId));
            }

            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                var oldInstance = _instanceLoader.LoadFrom(_repositoryDescription, oldCommitId);
                var newInstance = _instanceLoader.LoadFrom(_repositoryDescription, newCommitId);

                var oldCommit = repository.Lookup<Commit>(oldCommitId);
                var newCommit = repository.Lookup<Commit>(newCommitId);
                using (var changes = repository.Diff.Compare<TreeChanges>(oldCommit.Tree, newCommit.Tree))
                {
                    ThrowIfNonSupportedChangeTypes(changes);

                    var modified = CollectModifiedNodes(oldInstance, newInstance, changes, oldCommit);
                    var added = CollectAddedNodes(newInstance, changes, newCommit);
                    var deleted = CollectDeletedNodes(oldInstance, changes, oldCommit);
                    return new MetadataTreeChanges(newInstance, added.Concat(modified).Concat(deleted).ToImmutableList(), oldInstance);
                }
            });
        }

        static IImmutableList<MetadataTreeEntryChanges> CollectModifiedNodes(AbstractInstance oldInstance, AbstractInstance newInstance, TreeChanges changes, Commit oldCommit) =>
            (from c in changes.Where(c => c.Status == ChangeKind.Modified)
             let oldEntry = oldCommit[c.Path]
             where oldEntry.TargetType == TreeEntryTargetType.Blob
             let path = c.Path.GetParentPath()
             let oldNode = oldInstance.TryGetFromGitPath(path) ?? throw new NotSupportedException($"Node {path} could not be found in old instance.")
             let newNode = newInstance.TryGetFromGitPath(path) ?? throw new NotSupportedException($"Node {path} could not be found in new instance.")
             select new MetadataTreeEntryChanges(c.Path, c.Status, oldNode, newNode))
            .ToImmutableList();

        static IImmutableList<MetadataTreeEntryChanges> CollectAddedNodes(AbstractInstance newInstance, TreeChanges changes, Commit newCommit) =>
            (from c in changes.Where(c => c.Status == ChangeKind.Added)
             let newEntry = newCommit[c.Path]
             where newEntry.TargetType == TreeEntryTargetType.Blob
             let path = c.Path.GetParentPath()
             let newNode = newInstance.TryGetFromGitPath(path) ?? throw new NotSupportedException($"Node {path} could not be found in new instance.")
             select new MetadataTreeEntryChanges(c.Path, c.Status, null, newNode))
            .ToImmutableList();

        static IImmutableList<MetadataTreeEntryChanges> CollectDeletedNodes(AbstractInstance oldInstance, TreeChanges changes, Commit oldCommit) =>
            (from c in changes.Where(c => c.Status == ChangeKind.Deleted)
             let oldEntry = oldCommit[c.Path]
             where oldEntry.TargetType == TreeEntryTargetType.Blob
             let path = c.Path.GetParentPath()
             let oldNode = oldInstance.TryGetFromGitPath(path) ?? throw new NotSupportedException($"Node {path} could not be found in old instance.")
             select new MetadataTreeEntryChanges(c.Path, c.Status, oldNode, null))
            .ToImmutableList();

        static void ThrowIfNonSupportedChangeTypes(TreeChanges changes)
        {
            if (changes.Any(c => c.Status == ChangeKind.Conflicted))
            {
                throw new NotSupportedException("Conflicting changes is not yet supported.");
            }
            if (changes.Any(c => c.Status == ChangeKind.Copied))
            {
                throw new NotSupportedException("Copied changes is not yet supported.");
            }
            if (changes.Any(c => c.Status == ChangeKind.Renamed))
            {
                throw new NotSupportedException("Renamed changes is not yet supported.");
            }
            if (changes.Any(c => c.Status == ChangeKind.TypeChanged))
            {
                throw new NotSupportedException("TypeChanged changes is not yet supported.");
            }
        }

        /// <inheritdoc/>
        public MetadataTreeChanges Compare(IInstance original, IInstance newInstance)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }
            if (newInstance == null)
            {
                throw new ArgumentNullException(nameof(newInstance));
            }

            var changes = new List<MetadataTreeEntryChanges>();
            CompareNode(original, newInstance, changes, new Stack<string>());
            return new MetadataTreeChanges(newInstance, changes.ToImmutableList(), original);
        }

        void CompareNode(IMetadataObject original, IMetadataObject @new, IList<MetadataTreeEntryChanges> changes, Stack<string> stack)
        {
            var accessor = _modelDataProvider.Get(original.GetType());
            UpdateNodeIfNeeded(original, @new, stack, accessor, changes);
            foreach (var childProperty in accessor.ChildProperties)
            {
                if (!childProperty.ShouldVisitChildren(original) && !childProperty.ShouldVisitChildren(@new))
                {
                    // Do not visit children if they were not generated.
                    // This means that they were not modified!
                    continue;
                }

                stack.Push(childProperty.FolderName);
                CompareNodeChildren(original, @new, changes, stack, childProperty);
                stack.Pop();
            }
        }

        void CompareNodeChildren(IMetadataObject original, IMetadataObject @new, IList<MetadataTreeEntryChanges> changes, Stack<string> stack, ChildPropertyInfo childProperty)
        {
            using (var enumerator = new TwoSequenceEnumerator<IMetadataObject>(
                childProperty.Accessor(original),
                childProperty.Accessor(@new)))
            {
                while (!enumerator.BothCompleted)
                {
                    CompareNodeChildren(changes, stack, enumerator);
                }
            }
        }

        void CompareNodeChildren(IList<MetadataTreeEntryChanges> changes, Stack<string> stack, TwoSequenceEnumerator<IMetadataObject> enumerator)
        {
            if (enumerator.NodeIsStillThere)
            {
                stack.Push(enumerator.Left.Id.ToString());
                CompareNode(enumerator.Left, enumerator.Right, changes, stack);
                stack.Pop();

                enumerator.MoveNextLeft();
                enumerator.MoveNextRight();
                return;
            }
            else if (enumerator.NodeHasBeenAdded)
            {
                stack.Push(enumerator.Right.Id.ToString());
                var path = stack.ToDataPath();
                changes.Add(new MetadataTreeEntryChanges(path, ChangeKind.Added, @new: enumerator.Right));
                stack.Pop();
                enumerator.MoveNextRight();
                return;
            }
            else if (enumerator.NodeHasBeenRemoved)
            {
                stack.Push(enumerator.Left.Id.ToString());
                RemoveNode(enumerator.Left, changes, stack);
                stack.Pop();
                enumerator.MoveNextLeft();
                return;
            }
            throw new NotSupportedException("Unexpected child changes.");
        }
    }
}