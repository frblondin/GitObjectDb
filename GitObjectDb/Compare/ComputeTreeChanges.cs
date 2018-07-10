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
        readonly StringBuilder _jsonBuffer = new StringBuilder();

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

        void RemoveNode(IMetadataObject left, TreeDefinition definition, Stack<string> stack)
        {
            var path = stack.ToDataPath();
            definition.Remove(path);
            var dataAccessor = _modelDataProvider.Get(left.GetType());
            foreach (var childProperty in dataAccessor.ChildProperties)
            {
                stack.Push(childProperty.Name);
                foreach (var child in left.Children)
                {
                    stack.Push(child.Id.ToString());
                    RemoveNode(child, definition, stack);
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
                    return new MetadataTreeChanges(oldInstance, newInstance, added, modified, deleted);
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
             select new MetadataTreeEntryChanges(c, oldNode, newNode))
            .ToImmutableList();

        static IImmutableList<MetadataTreeEntryChanges> CollectAddedNodes(AbstractInstance newInstance, TreeChanges changes, Commit newCommit) =>
            (from c in changes.Where(c => c.Status == ChangeKind.Added)
             let newEntry = newCommit[c.Path]
             where newEntry.TargetType == TreeEntryTargetType.Blob
             let path = c.Path.GetParentPath()
             let newNode = newInstance.TryGetFromGitPath(path) ?? throw new NotSupportedException($"Node {path} could not be found in new instance.")
             select new MetadataTreeEntryChanges(c, null, newNode))
            .ToImmutableList();

        static IImmutableList<MetadataTreeEntryChanges> CollectDeletedNodes(AbstractInstance oldInstance, TreeChanges changes, Commit oldCommit) =>
            (from c in changes.Where(c => c.Status == ChangeKind.Deleted)
             let oldEntry = oldCommit[c.Path]
             where oldEntry.TargetType == TreeEntryTargetType.Blob
             let path = c.Path.GetParentPath()
             let oldNode = oldInstance.TryGetFromGitPath(path) ?? throw new NotSupportedException($"Node {path} could not be found in old instance.")
             select new MetadataTreeEntryChanges(c, oldNode, null))
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
        public (TreeDefinition NewTree, bool AnyChange) Compare(AbstractInstance original, AbstractInstance newInstance)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }
            if (newInstance == null)
            {
                throw new ArgumentNullException(nameof(newInstance));
            }

            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                var commit = repository.Lookup<Commit>(original.CommitId);
                var definition = TreeDefinition.From(commit);
                var stack = new Stack<string>();
                var anyChange = CompareNode(repository, original, newInstance, commit.Tree, definition, stack);
                return (definition, anyChange);
            });
        }

        bool CompareNode(IRepository repository, IMetadataObject original, IMetadataObject @new, Tree tree, TreeDefinition definition, Stack<string> stack)
        {
            var anyChange = false;
            var accessor = _modelDataProvider.Get(original.GetType());
            UpdateNodeIfNeeded(repository, original, @new, definition, stack, accessor, ref anyChange);
            foreach (var childProperty in accessor.ChildProperties)
            {
                if (!childProperty.ShouldVisitChildren(original) && !childProperty.ShouldVisitChildren(@new))
                {
                    // Do not visit children if they were not generated.
                    // This means that they were not modified!
                    continue;
                }

                stack.Push(childProperty.FolderName);
                anyChange |= CompareNodeChildren(repository, original, @new, tree, definition, stack, childProperty);
                stack.Pop();
            }
            return anyChange;
        }

        bool CompareNodeChildren(IRepository repository, IMetadataObject original, IMetadataObject @new, Tree tree, TreeDefinition definition, Stack<string> stack, ChildPropertyInfo childProperty)
        {
            var anyChange = false;
            using (var enumerator = new TwoSequenceEnumerator<IMetadataObject>(
                childProperty.Accessor(original),
                childProperty.Accessor(@new)))
            {
                while (!enumerator.BothCompleted)
                {
                    anyChange |= CompareNodeChildren(repository, tree, definition, stack, enumerator);
                }
            }
            return anyChange;
        }

        bool CompareNodeChildren(IRepository repository, Tree tree, TreeDefinition definition, Stack<string> stack, TwoSequenceEnumerator<IMetadataObject> enumerator)
        {
            if (enumerator.NodeIsStillThere)
            {
                stack.Push(enumerator.Left.Id.ToString());
                var anyChange = CompareNode(repository, enumerator.Left, enumerator.Right, tree, definition, stack);
                stack.Pop();

                enumerator.MoveNextLeft();
                enumerator.MoveNextRight();
                return anyChange;
            }
            else if (enumerator.NodeHasBeenAdded)
            {
                stack.Push(enumerator.Right.Id.ToString());
                AddOrUpdateNode(repository, enumerator.Right, definition, stack);
                stack.Pop();
                enumerator.MoveNextRight();
                return true;
            }
            else if (enumerator.NodeHasBeenRemoved)
            {
                stack.Push(enumerator.Left.Id.ToString());
                RemoveNode(enumerator.Left, definition, stack);
                stack.Pop();
                enumerator.MoveNextLeft();
                return true;
            }
            throw new NotSupportedException("Unexpected child changes.");
        }

        void UpdateNodeIfNeeded(IRepository repository, IMetadataObject original, IMetadataObject @new, TreeDefinition definition, Stack<string> stack, IModelDataAccessor accessor, ref bool anyChange)
        {
            if (accessor.ModifiableProperties.Any(p => !p.AreSame(original, @new)))
            {
                AddOrUpdateNode(repository, @new, definition, stack);
                anyChange = true;
            }
        }

        void AddOrUpdateNode(IRepository repository, IMetadataObject node, TreeDefinition definition, Stack<string> stack)
        {
            var path = stack.ToDataPath();
            node.ToJson(_jsonBuffer);
            definition.Add(path, repository.CreateBlob(_jsonBuffer), Mode.NonExecutableFile);
        }
    }
}