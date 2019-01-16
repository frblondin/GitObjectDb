using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitObjectDb.Services
{
    /// <inheritdoc/>
    internal class ComputeTreeChanges : IComputeTreeChanges
    {
        private readonly IModelDataAccessorProvider _modelDataProvider;
        private readonly IObjectRepositoryLoader _objectRepositoryLoader;
        private readonly IRepositoryProvider _repositoryProvider;
        private readonly ILogger<ComputeTreeChanges> _logger;
        private readonly IObjectRepositoryContainer _container;
        private readonly RepositoryDescription _repositoryDescription;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeTreeChanges"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="modelDataProvider">The model data provider.</param>
        /// <param name="objectRepositoryLoader">The object repository loader.</param>
        /// <param name="repositoryProvider">The repository provider.</param>
        /// <param name="logger">The logger.</param>
        [ActivatorUtilitiesConstructor]
        public ComputeTreeChanges(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription,
            IModelDataAccessorProvider modelDataProvider, IObjectRepositoryLoader objectRepositoryLoader,
            IRepositoryProvider repositoryProvider, ILogger<ComputeTreeChanges> logger)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _repositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));

            _modelDataProvider = modelDataProvider ?? throw new ArgumentNullException(nameof(modelDataProvider));
            _objectRepositoryLoader = objectRepositoryLoader ?? throw new ArgumentNullException(nameof(objectRepositoryLoader));
            _repositoryProvider = repositoryProvider ?? throw new ArgumentNullException(nameof(repositoryProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static void UpdateNodeIfNeeded(IModelObject original, IModelObject @new, Stack<string> stack, IModelDataAccessor accessor, IList<ObjectRepositoryEntryChanges> changes)
        {
            if (accessor.ModifiableProperties.Any(p => !p.AreSame(original, @new)))
            {
                var path = stack.ToDataPath();
                changes.Add(new ObjectRepositoryEntryChanges(path, ChangeKind.Modified, original, @new));
            }
        }

        private void RemoveNode(IModelObject left, IList<ObjectRepositoryEntryChanges> changed, Stack<string> stack)
        {
            var path = stack.ToDataPath();
            changed.Add(new ObjectRepositoryEntryChanges(path, ChangeKind.Deleted, old: left));
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
        public ObjectRepositoryChangeCollection Compare(ObjectId oldCommitId, ObjectId newCommitId)
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
                var oldRepository = _objectRepositoryLoader.LoadFrom(_container, _repositoryDescription, oldCommitId);
                var newRepository = _objectRepositoryLoader.LoadFrom(_container, _repositoryDescription, newCommitId);

                var oldCommit = repository.Lookup<Commit>(oldCommitId);
                var newCommit = repository.Lookup<Commit>(newCommitId);
                using (var changes = repository.Diff.Compare<TreeChanges>(oldCommit.Tree, newCommit.Tree))
                {
                    ThrowIfNonSupportedChangeTypes(changes);

                    var modified = CollectModifiedNodes(oldRepository, newRepository, changes, oldCommit);
                    var added = CollectAddedNodes(newRepository, changes, newCommit);
                    var deleted = CollectDeletedNodes(oldRepository, changes, oldCommit);

                    _logger.ChangesComputed(modified.Count, added.Count, deleted.Count, oldCommitId, newCommitId);

                    return new ObjectRepositoryChangeCollection(newRepository, added.Concat(modified).Concat(deleted).ToImmutableList(), oldRepository);
                }
            });
        }

        private static IImmutableList<ObjectRepositoryEntryChanges> CollectModifiedNodes(IObjectRepository oldRepository, IObjectRepository newRepository, TreeChanges changes, Commit oldCommit) =>
            (from c in changes.Where(c => c.Status == ChangeKind.Modified)
             let oldEntry = oldCommit[c.Path]
             where oldEntry.TargetType == TreeEntryTargetType.Blob
             let path = c.Path.GetParentPath()
             let oldNode = oldRepository.TryGetFromGitPath(path) ?? throw new ObjectNotFoundException($"Node {path} could not be found in old repository.")
             let newNode = newRepository.TryGetFromGitPath(path) ?? throw new ObjectNotFoundException($"Node {path} could not be found in new repository.")
             where !oldNode.Equals(newNode) // If new property have been defined in the data model we could end up with text differences even is object have same value (default values for ex.) = false positivies
             select new ObjectRepositoryEntryChanges(c.Path, c.Status, oldNode, newNode))
            .ToImmutableList();

        private static IImmutableList<ObjectRepositoryEntryChanges> CollectAddedNodes(IObjectRepository newRepository, TreeChanges changes, Commit newCommit) =>
            (from c in changes.Where(c => c.Status == ChangeKind.Added)
             let newEntry = newCommit[c.Path]
             where newEntry.TargetType == TreeEntryTargetType.Blob
             let path = c.Path.GetParentPath()
             let newNode = newRepository.TryGetFromGitPath(path) ?? throw new ObjectNotFoundException($"Node {path} could not be found in new instance.")
             select new ObjectRepositoryEntryChanges(c.Path, c.Status, null, newNode))
            .ToImmutableList();

        private static IImmutableList<ObjectRepositoryEntryChanges> CollectDeletedNodes(IObjectRepository oldRepository, TreeChanges changes, Commit oldCommit) =>
            (from c in changes.Where(c => c.Status == ChangeKind.Deleted)
             let oldEntry = oldCommit[c.Path]
             where oldEntry.TargetType == TreeEntryTargetType.Blob
             let path = c.Path.GetParentPath()
             let oldNode = oldRepository.TryGetFromGitPath(path) ?? throw new ObjectNotFoundException($"Node {path} could not be found in old instance.")
             select new ObjectRepositoryEntryChanges(c.Path, c.Status, oldNode, null))
            .ToImmutableList();

        private static void ThrowIfNonSupportedChangeTypes(TreeChanges changes)
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
        public ObjectRepositoryChangeCollection Compare(IObjectRepository original, IObjectRepository newRepository)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }
            if (newRepository == null)
            {
                throw new ArgumentNullException(nameof(newRepository));
            }

            var changes = new List<ObjectRepositoryEntryChanges>();
            CompareNode(original, newRepository, changes, new Stack<string>());
            var result = new ObjectRepositoryChangeCollection(newRepository, changes.ToImmutableList(), original);

            _logger.ChangesComputed(result.Modified.Count, result.Added.Count, result.Deleted.Count, original.CommitId, original.CommitId);

            return result;
        }

        private void CompareNode(IModelObject original, IModelObject @new, IList<ObjectRepositoryEntryChanges> changes, Stack<string> stack)
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

        private void CompareNodeChildren(IModelObject original, IModelObject @new, IList<ObjectRepositoryEntryChanges> changes, Stack<string> stack, ChildPropertyInfo childProperty)
        {
            using (var enumerator = new TwoSequenceEnumerator<IModelObject>(
                childProperty.Accessor(original),
                childProperty.Accessor(@new)))
            {
                while (!enumerator.BothCompleted)
                {
                    CompareNodeChildren(changes, stack, enumerator);
                }
            }
        }

        private void CompareNodeChildren(IList<ObjectRepositoryEntryChanges> changes, Stack<string> stack, TwoSequenceEnumerator<IModelObject> enumerator)
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
                changes.Add(new ObjectRepositoryEntryChanges(path, ChangeKind.Added, @new: enumerator.Right));
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

        /// <inheritdoc/>
        public ObjectRepositoryChangeCollection Compute(IObjectRepository repository, IList<ObjectRepositoryChunkChange> modifiedChunks, IList<ObjectRepositoryAdd> addedObjects, IList<ObjectRepositoryDelete> deletedObjects)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (modifiedChunks == null)
            {
                throw new ArgumentNullException(nameof(modifiedChunks));
            }
            if (addedObjects == null)
            {
                throw new ArgumentNullException(nameof(addedObjects));
            }
            if (deletedObjects == null)
            {
                throw new ArgumentNullException(nameof(deletedObjects));
            }

            var changes = modifiedChunks.ToLookup(c => c.Path, StringComparer.OrdinalIgnoreCase);
            var allImpactedPaths =
                modifiedChunks.Select(c => c.Path)
                .Union(addedObjects.Select(a => a.Path), StringComparer.OrdinalIgnoreCase)
                .Union(deletedObjects.Select(d => d.Path), StringComparer.OrdinalIgnoreCase);
            var tempId = default(UniqueId);
            var forceVisit = new HashSet<UniqueId>(from path in allImpactedPaths
                                                from part in path.Split('/')
                                                where UniqueId.TryParse(part, out tempId)
                                                let id = tempId
                                                select id);
            object ProcessProperty(IModelObject node, string name, Type argumentType, object fallback)
            {
                var path = node.GetDataPath();
                var propertyChange = changes[path].TryGetWithValue(c => c.Property.Name, name);
                if (propertyChange != null)
                {
                    return propertyChange.MergeValue;
                }
                else
                {
                    return fallback is ICloneable cloneable ? cloneable.Clone() : fallback;
                }
            }

            (IEnumerable<IModelObject> Additions, IEnumerable<IModelObject> Deletions) ChildChangesGetter(IModelObject node, ChildPropertyInfo childProperty)
            {
                var pathWithProperty = GetChildPathRegex(node, childProperty);
                var additions = (from o in addedObjects
                                 where pathWithProperty.IsMatch(o.Path)
                                 select o.Child).ToList();
                var deleted = new HashSet<UniqueId>(from o in deletedObjects
                                                    where pathWithProperty.IsMatch(o.Path)
                                                    select o.Id);
                var deletions = childProperty.Accessor(node).Where(n => deleted.Contains(n.Id)).ToList();

                return (additions, deletions);
            }

            var mergeResult = repository.DataAccessor.DeepClone(repository, ProcessProperty, ChildChangesGetter, n => forceVisit.Contains(n.Id));
            return Compare(repository, mergeResult);
        }

        private static Regex GetChildPathRegex(IModelObject node, ChildPropertyInfo childProperty)
        {
            var path = node.GetFolderPath();
            return string.IsNullOrEmpty(path) ?
                new Regex($@"{childProperty.FolderName}/[\w-]+/{FileSystemStorage.DataFile}", RegexOptions.IgnoreCase) :
                new Regex($@"{path}/{childProperty.FolderName}/[\w-]+/{FileSystemStorage.DataFile}", RegexOptions.IgnoreCase);
        }
    }
}