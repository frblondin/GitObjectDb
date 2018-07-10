using GitObjectDb.Attributes;
using GitObjectDb.Git;
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

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Applies the merge changes.
    /// </summary>
    internal sealed class MetadataTreeMergeProcessor
    {
        readonly IRepositoryProvider _repositoryProvider;
        readonly RepositoryDescription _repositoryDescription;
        readonly Lazy<JsonSerializer> _serializer;

        readonly MetadataTreeMerge _metadataTreeMerge;
        readonly StringBuilder _buffer = new StringBuilder();

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
            _serializer = new Lazy<JsonSerializer>(() => serviceProvider.GetRequiredService<IInstanceLoader>().GetJsonSerializer());
        }

        static JObject GetContent(Commit mergeBase, string path, string branchInfo)
        {
            var content = mergeBase[path]?.Target?.Peel<Blob>()?.GetContentText() ??
                throw new NotImplementedException($"Could not find node {path} in {branchInfo} tree.");
            return JsonConvert.DeserializeObject<JObject>(content);
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
            var branch = repository.Branches[_metadataTreeMerge.BranchName];
            var treeDefinition = CreateTree(repository);
            var message = $"Merge branch {branch.FriendlyName} into {repository.Head.FriendlyName}";
            var commit = repository.Commit(treeDefinition, message, merger, merger, mergeParent: repository.Lookup<Commit>(_metadataTreeMerge.BranchTarget));
            return commit.Id;
        }

        TreeDefinition CreateTree(IRepository repository)
        {
            var definition = TreeDefinition.From(repository.Head.Tip);

            ManageModifications(repository, definition);
            ManageAdditions(repository, definition);
            ManageDeletions(definition);

            return definition;
        }

        void ManageModifications(IRepository repository, TreeDefinition definition)
        {
            foreach (var change in _metadataTreeMerge.ModifiedChunks.GroupBy(c => c.HeadNode))
            {
                var modified = (JObject)change.Key.DeepClone();
                foreach (var chunkChange in change)
                {
                    chunkChange.ApplyTo(modified);
                }
                Serialize(modified);
                definition.Add(change.First().Path, repository.CreateBlob(_buffer), Mode.NonExecutableFile);
            }
        }

        void ManageAdditions(IRepository repository, TreeDefinition definition)
        {
            foreach (var addition in _metadataTreeMerge.AddedObjects)
            {
                Serialize(addition.BranchNode);
                definition.Add(addition.Path, repository.CreateBlob(_buffer), Mode.NonExecutableFile);
            }
        }

        void ManageDeletions(TreeDefinition definition)
        {
            foreach (var deletion in _metadataTreeMerge.DeletedObjects)
            {
                definition.Remove(deletion.Path);
            }
        }

        void Serialize(JToken modified)
        {
            _buffer.Clear();
            using (var writer = new StringWriter(_buffer))
            {
                _serializer.Value.Serialize(writer, modified);
            }
        }
    }
}
