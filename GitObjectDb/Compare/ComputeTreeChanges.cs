using Autofac;
using LibGit2Sharp;
using GitObjectDb.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using GitObjectDb.Models;

namespace GitObjectDb.Compare
{
    public class ComputeTreeChanges
    {
        readonly IComponentContext _context;
        readonly IModelDataAccessorProvider _provider;
        readonly Func<Repository> _repositoryFactory;

        public delegate ComputeTreeChanges Factory(Func<Repository> repositoryFactory);
        public ComputeTreeChanges(IComponentContext context, IModelDataAccessorProvider provider, Func<Repository> repositoryFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _repositoryFactory = repositoryFactory;
        }

        public MetadataTreeChanges Compare<TInstance>(Func<Repository, Tree> oldTreeGetter, Func<Repository, Tree> newTreeGetter) where TInstance : AbstractInstance
        {
            var oldModule = InstanceLoader.LoadFrom<TInstance>(_context, _repositoryFactory, oldTreeGetter);
            var newModule = InstanceLoader.LoadFrom<TInstance>(_context, _repositoryFactory, newTreeGetter);

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
            var anyChange = false;
            CompareNode(original, @new, repository, tree, definition, stack, ref anyChange);
            return (definition, anyChange);
        }

        void CompareNode(IMetadataObject original, IMetadataObject @new, Repository repository, Tree tree, TreeDefinition definition, Stack<string> stack, ref bool anyChange)
        {
            var accessor = _provider.Get(original.GetType());
            UpdateNodeIfNeeded(original, @new, repository, definition, stack, accessor, ref anyChange);
            foreach (var childProperty in accessor.ChildProperties)
            {
                if (!childProperty.ShouldVisitChildren(original) && !childProperty.ShouldVisitChildren(@new))
                {
                    // Do not visit children if they were not generated.
                    // This means that they were not modified!
                    continue;
                }

                var originalChildren = childProperty.Accessor(original);
                var newChildren = childProperty.Accessor(@new);
                stack.Push(childProperty.Property.Name);
                int posOriginal = 0, posNew = 0;
                while (true)
                {
                    var originalChild = originalChildren.Count > posOriginal ? (IMetadataObject)originalChildren[posOriginal] : null;
                    var newChild = newChildren.Count > posNew ? (IMetadataObject)newChildren[posNew] : null;
                    if (originalChild == null && newChild == null) break;
                    CompareNodeChildren(repository, tree, definition, stack, originalChild, newChild, ref posOriginal, ref posNew, ref anyChange);
                }
                stack.Pop();
            }
        }

        void CompareNodeChildren(Repository repository, Tree tree, TreeDefinition definition, Stack<string> stack, IMetadataObject originalChild, IMetadataObject newChild, ref int posOriginal, ref int posNew, ref bool anyChange)
        {
            if (NodeIsStillThere(originalChild, newChild))
            {
                stack.Push(originalChild.Id.ToString());
                CompareNode(originalChild, newChild, repository, tree, definition, stack, ref anyChange);
                stack.Pop();

                posNew++;
                posOriginal++;
                return;
            }
            else if (NodeHasBeenAdded(originalChild, newChild))
            {
                AddOrUpdateNode(newChild, repository, definition, stack);
                anyChange = true;
                posNew++;
                return;
            }
            else if (NodeHasBeenRemoved(originalChild, newChild))
            {
                RemoveNode(originalChild, definition, stack);
                anyChange = true;
                posOriginal++;
                return;
            }
            throw new NotSupportedException("Unexpected child changes.");
        }

        static bool NodeIsStillThere(IMetadataObject originalChild, IMetadataObject newChild) =>
            newChild != null && originalChild != null &&
            originalChild.CompareTo(newChild) == 0;
        static bool NodeHasBeenAdded(IMetadataObject originalChild, IMetadataObject newChild) =>
            originalChild == null && newChild != null ||
            (originalChild != null && originalChild.CompareTo(newChild) > 0);
        static bool NodeHasBeenRemoved(IMetadataObject originalChild, IMetadataObject newChild) =>
            originalChild != null && newChild == null ||
            (newChild != null && originalChild.CompareTo(newChild) < 0);

        static void UpdateNodeIfNeeded(IMetadataObject original, IMetadataObject @new, Repository repository, TreeDefinition definition, Stack<string> stack, IModelDataAccessor accessor, ref bool anyChange)
        {
            if (accessor.ModifiableProperties.Any(p => !p.AreSame(original, @new)))
            {
                AddOrUpdateNode(@new, repository, definition, stack);
                anyChange = true;
            }
        }

        static void AddOrUpdateNode(IMetadataObject node, Repository repository, TreeDefinition definition, Stack<string> stack)
        {
            var path = GetDataPath(stack);
            definition.Add(path, repository.CreateBlob(node.ToJson()), Mode.NonExecutableFile);
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
