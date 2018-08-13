using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Migrations;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Applies the merge changes.
    /// </summary>
    internal sealed class MetadataTreeMergeProcessor
    {
        readonly IRepositoryProvider _repositoryProvider;
        readonly Func<RepositoryDescription, IComputeTreeChanges> _computeTreeChangesFactory;
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
            _computeTreeChangesFactory = serviceProvider.GetRequiredService<Func<RepositoryDescription, IComputeTreeChanges>>();
            _serializer = new Lazy<JsonSerializer>(() => serviceProvider.GetRequiredService<IObjectRepositoryLoader>().GetJsonSerializer());
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

                _metadataTreeMerge.RequiredMigrator?.Apply();
                return ApplyMerge(merger, repository);
            });
        }

        ObjectId ApplyMerge(Signature merger, IRepository repository)
        {
            var treeChanges = ComputeMergeResult();

            var branch = repository.Branches[_metadataTreeMerge.BranchName];
            var message = $"Merge branch {branch.FriendlyName} into {repository.Head.FriendlyName}";
            var commit = repository.CommitChanges(treeChanges, message, merger, merger, hooks: _hooks, mergeParent: repository.Lookup<Commit>(_metadataTreeMerge.BranchTarget));
            return commit?.Id;
        }

        MetadataTreeChanges ComputeMergeResult()
        {
            var mergeResult = _metadataTreeMerge.Repository.DataAccessor.DeepClone(_metadataTreeMerge.Repository, ProcessProperty, ChildChangesGetter, n => _forceVisit.Contains(n.Id));
            var computeChanges = _computeTreeChangesFactory(_repositoryDescription);
            return computeChanges.Compare(_metadataTreeMerge.Repository, mergeResult);
        }

        object ProcessProperty(IMetadataObject node, string name, Type argumentType, object fallback)
        {
            var path = node.GetDataPath();
            var propertyChange = _changes[path].FirstOrDefault(c => c.Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
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
