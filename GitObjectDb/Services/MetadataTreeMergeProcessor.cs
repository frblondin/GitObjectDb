using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitObjectDb.Services
{
    /// <summary>
    /// Applies the merge changes.
    /// </summary>
    internal sealed class MetadataTreeMergeProcessor
    {
        readonly IRepositoryProvider _repositoryProvider;
        readonly ComputeTreeChangesFactory _computeTreeChangesFactory;
        readonly RepositoryDescription _repositoryDescription;
        readonly Lazy<JsonSerializer> _serializer;
        readonly GitHooks _hooks;

        readonly MetadataTreeMerge _metadataTreeMerge;
        readonly ISet<Guid> _forceVisit;
        readonly ILookup<string, MetadataTreeMergeChunkChange> _changes;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeMergeProcessor"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="metadataTreeMerge">The metadata tree merge.</param>
        /// <exception cref="ArgumentNullException">
        /// serviceProvider
        /// or
        /// repositoryDescription
        /// </exception>
        internal MetadataTreeMergeProcessor(IServiceProvider serviceProvider, RepositoryDescription repositoryDescription, MetadataTreeMerge metadataTreeMerge)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _repositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));
            _metadataTreeMerge = metadataTreeMerge ?? throw new ArgumentNullException(nameof(metadataTreeMerge));

            _repositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();
            _computeTreeChangesFactory = serviceProvider.GetRequiredService<ComputeTreeChangesFactory>();
            _serializer = new Lazy<JsonSerializer>(() => serviceProvider.GetRequiredService<IObjectRepositoryLoader>().GetJsonSerializer(metadataTreeMerge.Container));
            _hooks = serviceProvider.GetRequiredService<GitHooks>();
            _changes = _metadataTreeMerge.ModifiedChunks.ToLookup(c => c.Path, StringComparer.OrdinalIgnoreCase);

            Guid tempGuid;
            _forceVisit = new HashSet<Guid>(from path in _metadataTreeMerge.AllImpactedPaths
                                            from part in path.Split('/')
                                            where Guid.TryParse(part, out tempGuid)
                                            let guid = tempGuid
                                            select guid);
        }

        static Regex GetChildPathRegex(IMetadataObject node, ChildPropertyInfo childProperty)
        {
            var path = node.GetFolderPath();
            return string.IsNullOrEmpty(path) ?
                new Regex($@"{childProperty.FolderName}/[\w-]+/{FileSystemStorage.DataFile}", RegexOptions.IgnoreCase) :
                new Regex($@"{path}/{childProperty.FolderName}/[\w-]+/{FileSystemStorage.DataFile}", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Applies the specified merger.
        /// </summary>
        /// <param name="merger">The merger.</param>
        /// <returns>The merge commit id.</returns>
        internal ObjectId Apply(Signature merger)
        {
            if (merger == null)
            {
                throw new ArgumentNullException(nameof(merger));
            }
            var remainingConflicts = _metadataTreeMerge.ModifiedChunks.Where(c => c.IsInConflict).ToList();
            if (remainingConflicts.Any())
            {
                throw new RemainingConflictsException(remainingConflicts);
            }

            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                _metadataTreeMerge.EnsureHeadCommit(repository);

                var resultId = ApplyMerge(merger, repository);
                if (resultId == null)
                {
                    return null;
                }
                _metadataTreeMerge.RequiredMigrator?.Apply();
                return resultId;
            });
        }

        ObjectId ApplyMerge(Signature merger, IRepository repository)
        {
            var treeChanges = ComputeMergeResult();

            if (!_hooks.OnMergeStarted(treeChanges))
            {
                return null;
            }

            var commit = CommitChanges(merger, repository, treeChanges);
            if (_metadataTreeMerge.Repository.Container is ObjectRepositoryContainer container)
            {
                container.ReloadRepository(_metadataTreeMerge.Repository, commit);
            }

            _hooks.OnMergeCompleted(treeChanges, commit);

            return commit;
        }

        ObjectId CommitChanges(Signature merger, IRepository repository, MetadataTreeChanges treeChanges)
        {
            if (_metadataTreeMerge.RequiresMergeCommit)
            {
                var message = $"Merge branch {_metadataTreeMerge.BranchName} into {repository.Head.FriendlyName}";
                return repository.CommitChanges(treeChanges, message, merger, merger, hooks: _hooks, mergeParent: repository.Lookup<Commit>(_metadataTreeMerge.MergeCommitId)).Id;
            }
            else
            {
                var commit = repository.Lookup<Commit>(_metadataTreeMerge.MergeCommitId);
                var logMessage = commit.BuildCommitLogMessage(false, false, false);
                repository.UpdateHeadAndTerminalReference(commit, logMessage);
                return _metadataTreeMerge.MergeCommitId;
            }
        }

        MetadataTreeChanges ComputeMergeResult()
        {
            var mergeResult = _metadataTreeMerge.Repository.DataAccessor.DeepClone(_metadataTreeMerge.Repository, ProcessProperty, ChildChangesGetter, n => _forceVisit.Contains(n.Id));
            var computeChanges = _computeTreeChangesFactory(_metadataTreeMerge.Container, _repositoryDescription);
            return computeChanges.Compare(_metadataTreeMerge.Repository, mergeResult);
        }

        object ProcessProperty(IMetadataObject node, string name, Type argumentType, object fallback)
        {
            var path = node.GetDataPath();
            var propertyChange = _changes[path].TryGetWithValue(c => c.Property.Name, name);
            if (propertyChange != null)
            {
                return propertyChange.MergeValue.ToObject(argumentType, _serializer.Value);
            }
            else
            {
                return fallback is ICloneable cloneable ? cloneable.Clone() : fallback;
            }
        }

        (IEnumerable<IMetadataObject> Additions, IEnumerable<IMetadataObject> Deletions) ChildChangesGetter(IMetadataObject node, ChildPropertyInfo childProperty)
        {
            var pathWithProperty = GetChildPathRegex(node, childProperty);
            var additions = (from o in _metadataTreeMerge.AddedObjects
                             where pathWithProperty.IsMatch(o.Path)
                             let objectType = Type.GetType(o.BranchNode.Value<string>("$type"))
                             select (IMetadataObject)o.BranchNode.ToObject(childProperty.ItemType, _serializer.Value)).ToList();
            var deleted = new HashSet<Guid>(from o in _metadataTreeMerge.DeletedObjects
                                            where pathWithProperty.IsMatch(o.Path)
                                            select o.BranchNode["Id"].ToObject<Guid>());
            var deletions = childProperty.Accessor(node).Where(n => deleted.Contains(n.Id)).ToList();

            return (additions, deletions);
        }
    }
}
