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
    /// <summary>
    /// Compares to commits and computes the differences (additions, deletions...).
    /// </summary>
    public class ComputeTreeChanges
    {
        readonly IServiceProvider _serviceProvider;
        readonly IModelDataAccessorProvider _modelDataProvider;
        readonly IInstanceLoader _instanceLoader;
        readonly Func<Repository> _repositoryFactory;
        readonly StringBuilder _jsonBuffer = new StringBuilder();

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeTreeChanges"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="repositoryFactory">The repository factory.</param>
        /// <exception cref="ArgumentNullException">serviceProvider</exception>
        public ComputeTreeChanges(IServiceProvider serviceProvider, Func<Repository> repositoryFactory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _modelDataProvider = serviceProvider.GetService<IModelDataAccessorProvider>();
            _instanceLoader = serviceProvider.GetService<IInstanceLoader>();
            _repositoryFactory = repositoryFactory;
        }

        /// <summary>
        /// Provides support for creazting <see cref="ComputeTreeChanges"/> objects.
        /// </summary>
        /// <param name="repositoryFactory">The repository factory.</param>
        /// <returns>The <see cref="ComputeTreeChanges"/> instance.</returns>
        public delegate ComputeTreeChanges Factory(Func<Repository> repositoryFactory);

        static void RemoveNode(TreeDefinition definition, Stack<string> stack)
        {
            var path = stack.ToDataPath();
            definition.Remove(path);
        }

        /// <summary>
        /// Compares the specified commits.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance.</typeparam>
        /// <param name="oldTreeGetter">The old tree getter.</param>
        /// <param name="newTreeGetter">The new tree getter.</param>
        /// <returns>A <see cref="MetadataTreeChanges"/> containing all computed changes.</returns>
        public MetadataTreeChanges Compare<TInstance>(Func<Repository, Tree> oldTreeGetter, Func<Repository, Tree> newTreeGetter)
            where TInstance : AbstractInstance
        {
            var oldModule = _instanceLoader.LoadFrom<TInstance>(_repositoryFactory, oldTreeGetter);
            var newModule = _instanceLoader.LoadFrom<TInstance>(_repositoryFactory, newTreeGetter);

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
                return new MetadataTreeChanges(added, modified, deleted);
            }
        }

        static IImmutableList<MetadataTreeEntryChanges> CollectModifiedNodes(AbstractInstance oldInstance, AbstractInstance newInstance, TreeChanges changes, Tree oldTree) =>
            (from c in changes.Modified
             let oldEntry = oldTree[c.Path]
             where oldEntry.TargetType == TreeEntryTargetType.Blob
             let oldNode = oldInstance.TryGetFromGitPath(c.Path.ParentPath())
             let newNode = newInstance.TryGetFromGitPath(c.Path.ParentPath())
             select new MetadataTreeEntryChanges(oldNode, newNode))
            .ToImmutableList();

        static IImmutableList<MetadataTreeEntryChanges> CollectAddedNodes(AbstractInstance newInstance, TreeChanges changes, Tree newTree) =>
            (from c in changes.Added
             let newEntry = newTree[c.Path]
             where newEntry.TargetType == TreeEntryTargetType.Blob
             let newNode = newInstance.TryGetFromGitPath(c.Path.ParentPath())
             select new MetadataTreeEntryChanges(null, newNode))
            .ToImmutableList();

        static IImmutableList<MetadataTreeEntryChanges> CollectDeletedNodes(AbstractInstance oldInstance, TreeChanges changes, Tree oldTree) =>
            (from c in changes.Deleted
             let oldEntry = oldTree[c.Path]
             where oldEntry.TargetType == TreeEntryTargetType.Blob
             let oldNode = oldInstance.TryGetFromGitPath(c.Path.ParentPath())
             select new MetadataTreeEntryChanges(oldNode, null))
            .ToImmutableList();

        static void ThrowIfNonSupportedChangeTypes(TreeChanges changes)
        {
            if (changes.Conflicted.Any())
            {
                throw new NotSupportedException("Conflicting changes is not yet supported.");
            }
            if (changes.Copied.Any())
            {
                throw new NotSupportedException("Copied changes is not yet supported.");
            }
            if (changes.Renamed.Any())
            {
                throw new NotSupportedException("Renamed changes is not yet supported.");
            }
            if (changes.TypeChanged.Any())
            {
                throw new NotSupportedException("TypeChanged changes is not yet supported.");
            }
        }

        /// <summary>
        /// Compares two <see cref="AbstractInstance"/> instances and generates a new <see cref="TreeDefinition"/> instance containing the tree changes to be committed.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="new">The new.</param>
        /// <param name="repository">The repository.</param>
        /// <returns>The <see cref="TreeDefinition"/> and <code>true</code> is any change was detected between the two instances.</returns>
        /// <exception cref="ArgumentNullException">
        /// original
        /// or
        /// new
        /// </exception>
        internal (TreeDefinition NewTree, bool AnyChange) Compare(AbstractInstance original, AbstractInstance @new, Repository repository)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }
            if (@new == null)
            {
                throw new ArgumentNullException(nameof(@new));
            }

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
                RemoveNode(definition, stack);
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
            var path = stack.ToDataPath();
            node.ToJson(_jsonBuffer);
            definition.Add(path, repository.CreateBlob(_jsonBuffer), Mode.NonExecutableFile);
        }
    }
}
