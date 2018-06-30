using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using GitObjectDb.Models;
using GitObjectDb.Reflection;

namespace GitObjectDb.Compare
{
    public class ComputeTreeChanges
    {
        readonly IServiceProvider _serviceProvider;
        readonly IModelDataAccessorProvider _modelDataProvider;
        readonly Func<Repository> _repositoryFactory;
        readonly StringBuilder _jsonBuffer = new StringBuilder();

        public delegate ComputeTreeChanges Factory(Func<Repository> repositoryFactory);
        public ComputeTreeChanges(IServiceProvider serviceProvider, Func<Repository> repositoryFactory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _modelDataProvider = serviceProvider.GetService<IModelDataAccessorProvider>();
            _repositoryFactory = repositoryFactory;
        }

        public MetadataTreeChanges Compare<TInstance>(Func<Repository, Tree> oldTreeGetter, Func<Repository, Tree> newTreeGetter) where TInstance : AbstractInstance
        {
            var oldModule = InstanceLoader.LoadFrom<TInstance>(_serviceProvider, _repositoryFactory, oldTreeGetter);
            var newModule = InstanceLoader.LoadFrom<TInstance>(_serviceProvider, _repositoryFactory, newTreeGetter);

            TreeChanges changes;
            using (var repository = _repositoryFactory())
            {
                var oldTree = oldTreeGetter(repository);
                var newTree = newTreeGetter(repository);
                changes = repository.Diff.Compare<TreeChanges>(oldTree, newTree);

                ThrowIfNonSupportedChangeTypes(changes);

                var modified = CollectModifiedNodes(oldModule, newModule, changes, oldTree);
                var added = CollectAddedNodes(newModule, changes, newTree);
                var deleted = CollectDeletedNodes(oldModule, changes, oldTree);
                return new MetadataTreeChanges(modified, added, deleted);
            }
        }

        static IImmutableList<MetadataTreeEntryChanges> CollectModifiedNodes(AbstractInstance oldInstance, AbstractInstance newInstance, TreeChanges changes, Tree oldTree) =>
            (from c in changes.Modified
             let oldEntry = oldTree[c.Path]
             where oldEntry.TargetType == TreeEntryTargetType.Blob
             let oldNode = oldInstance.GetFromGitPath(c.Path.ParentPath())
             let newNode = newInstance.GetFromGitPath(c.Path.ParentPath())
             select new MetadataTreeEntryChanges(oldNode, newNode))
            .ToImmutableList();

        static IImmutableList<MetadataTreeEntryChanges> CollectAddedNodes(AbstractInstance newInstance, TreeChanges changes, Tree newTree) =>
            (from c in changes.Added
             let newEntry = newTree[c.Path]
             where newEntry.TargetType == TreeEntryTargetType.Blob
             let newNode = newInstance.GetFromGitPath(c.Path.ParentPath())
             select new MetadataTreeEntryChanges(null, newNode))
            .ToImmutableList();

        static IImmutableList<MetadataTreeEntryChanges> CollectDeletedNodes(AbstractInstance oldInstance, TreeChanges changes, Tree oldTree) =>
            (from c in changes.Deleted
             let oldEntry = oldTree[c.Path]
             where oldEntry.TargetType == TreeEntryTargetType.Blob
             let oldNode = oldInstance.GetFromGitPath(c.Path.ParentPath())
             select new MetadataTreeEntryChanges(oldNode, null))
            .ToImmutableList();

        static void ThrowIfNonSupportedChangeTypes(TreeChanges changes)
        {
            if (changes.Conflicted.Any()) throw new NotSupportedException("Conflicting changes is not yet supported.");
            if (changes.Copied.Any()) throw new NotSupportedException("Copied changes is not yet supported.");
            if (changes.Renamed.Any()) throw new NotSupportedException("Renamed changes is not yet supported.");
            if (changes.TypeChanged.Any()) throw new NotSupportedException("TypeChanged changes is not yet supported.");
        }

        internal (TreeDefinition NewTree, bool AnyChange) Compare(AbstractInstance original, AbstractInstance @new, Repository repository)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (@new == null) throw new ArgumentNullException(nameof(@new));

            var tree = original._getTree(repository);
            var definition = TreeDefinition.From(tree);
            var stack = new Stack<string>();
            var anyChange = CompareNode(original, @new, repository, tree, definition, stack);
            return (definition, anyChange);
        }

        bool CompareNode(IMetadataObject original, IMetadataObject @new, Repository repository, Tree tree, TreeDefinition definition, Stack<string> stack)
        {
            var anyChange = false;
            var accessor = _modelDataProvider.Get(original.GetType());
            UpdateNodeIfNeeded(original, @new, repository, definition, stack, accessor, ref anyChange);
            foreach (var childProperty in accessor.ChildProperties)
            {
                if (!childProperty.ShouldVisitChildren(original) && !childProperty.ShouldVisitChildren(@new))
                {
                    // Do not visit children if they were not generated.
                    // This means that they were not modified!
                    continue;
                }

                stack.Push(childProperty.Property.Name);
                anyChange |= CompareNodeChildren(original, @new, repository, tree, definition, stack, childProperty);
                stack.Pop();
            }
            return anyChange;
        }

        bool CompareNodeChildren(IMetadataObject original, IMetadataObject @new, Repository repository, Tree tree, TreeDefinition definition, Stack<string> stack, ChildPropertyInfo childProperty)
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

        bool CompareNodeChildren(Repository repository, Tree tree, TreeDefinition definition, Stack<string> stack, TwoSequenceEnumerator<IMetadataObject> enumerator)
        {
            if (enumerator.NodeIsStillThere)
            {
                stack.Push(enumerator.Left.Id.ToString());
                var anyChange = CompareNode(enumerator.Left, enumerator.Right, repository, tree, definition, stack);
                stack.Pop();

                enumerator.MoveNextLeft();
                enumerator.MoveNextRight();
                return anyChange;
            }
            else if (enumerator.NodeHasBeenAdded)
            {
                AddOrUpdateNode(enumerator.Right, repository, definition, stack);
                enumerator.MoveNextRight();
                return true;
            }
            else if (enumerator.NodeHasBeenRemoved)
            {
                RemoveNode(enumerator.Left, definition, stack);
                enumerator.MoveNextLeft();
                return true;
            }
            throw new NotSupportedException("Unexpected child changes.");
        }

        void UpdateNodeIfNeeded(IMetadataObject original, IMetadataObject @new, Repository repository, TreeDefinition definition, Stack<string> stack, IModelDataAccessor accessor, ref bool anyChange)
        {
            if (accessor.ModifiableProperties.Any(p => !p.AreSame(original, @new)))
            {
                AddOrUpdateNode(@new, repository, definition, stack);
                anyChange = true;
            }
        }

        void AddOrUpdateNode(IMetadataObject node, Repository repository, TreeDefinition definition, Stack<string> stack)
        {
            var path = GetDataPath(stack);
            node.ToJson(_jsonBuffer);
            definition.Add(path, repository.CreateBlob(_jsonBuffer), Mode.NonExecutableFile);
        }

        static void RemoveNode(IMetadataObject node, TreeDefinition definition, Stack<string> stack)
        {
            var path = GetDataPath(stack);
            definition.Remove(path);
        }

        static string GetDataPath(Stack<string> stack)
        {
            var path = stack.ToPath();
            if (!string.IsNullOrEmpty(path)) path += "/";
            path += InstanceLoader.DataFile;
            return path;
        }
    }
}
