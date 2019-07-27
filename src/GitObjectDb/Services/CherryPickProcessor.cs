using GitObjectDb.Models;
using GitObjectDb.Models.CherryPick;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Merge;
using GitObjectDb.Reflection;
using GitObjectDb.Serialization;
using GitObjectDb.Transformations;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Services
{
    /// <summary>
    /// Applies the cherry picked changes.
    /// </summary>
    internal class CherryPickProcessor
    {
        private readonly ObjectRepositoryCherryPick _cherryPick;

        private readonly ComputeTreeChangesFactory _computeTreeChangesFactory;
        private readonly IObjectRepositorySerializer _serializer;

        [ActivatorUtilitiesConstructor]
        internal CherryPickProcessor(ObjectRepositoryCherryPick objectRepositoryCherryPick,
            ComputeTreeChangesFactory computeTreeChangesFactory, ObjectRepositorySerializerFactory serializerFactory)
        {
            if (serializerFactory == null)
            {
                throw new ArgumentNullException(nameof(serializerFactory));
            }

            _cherryPick = objectRepositoryCherryPick ?? throw new ArgumentNullException(nameof(objectRepositoryCherryPick));

            _computeTreeChangesFactory = computeTreeChangesFactory ?? throw new ArgumentNullException(nameof(computeTreeChangesFactory));
            _serializer = serializerFactory(new ModelObjectSerializationContext(objectRepositoryCherryPick.Repository.Container));
        }

        /// <summary>
        /// Creates a new instance of <see cref="CherryPickProcessor"/>.
        /// </summary>
        /// <param name="objectRepositoryCherryPick">The object repository rebase.</param>
        /// <returns>The newly created instance.</returns>
        internal delegate CherryPickProcessor Factory(ObjectRepositoryCherryPick objectRepositoryCherryPick);

        internal (CherryPickStatus Status, IObjectRepository Result) Initialize(IRepository repository, Commit parentCommit)
        {
            ComputeChanges(repository, parentCommit);

            if (_cherryPick.ModifiedProperties.Any(c => c.IsInConflict))
            {
                return (CherryPickStatus.Conflicts, null);
            }
            else
            {
                return Complete(repository);
            }
        }

        internal (CherryPickStatus Status, IObjectRepository Result) Complete(IRepository repository)
        {
            if (_cherryPick.ModifiedProperties.Any(c => c.IsInConflict))
            {
                throw new GitObjectDbException("There are remaining unresolved conflicts.");
            }

            var transformations = new TransformationFromChunkChanges(_cherryPick.ModifiedProperties, _cherryPick.AddedObjects, _cherryPick.DeletedObjects);
            var transformed = _cherryPick.Repository.DataAccessor.With(_cherryPick.Repository, transformations);
            transformed.SetRepositoryData(_cherryPick.Repository.RepositoryDescription, ObjectId.Zero);

            var computeChanges = _computeTreeChangesFactory(_cherryPick.Repository.Container, _cherryPick.Repository.RepositoryDescription);
            var lastCommit = repository.Lookup<Commit>(_cherryPick.HeadCommitId);
            var cherryPickCommit = repository.Lookup<Commit>(_cherryPick.CherryPickCommitId);
            var changes = computeChanges.Compare(_cherryPick.Repository, transformed.Repository);
            if (changes.Any())
            {
                var definition = TreeDefinition.From(lastCommit);
                repository.UpdateTreeDefinition(changes, definition, _serializer, lastCommit);
                var tree = repository.ObjectDatabase.CreateTree(definition);
                lastCommit = repository.ObjectDatabase.CreateCommit(cherryPickCommit.Author, cherryPickCommit.Committer, cherryPickCommit.Message, tree, new[] { lastCommit }, false);
            }

            var logMessage = lastCommit.BuildCommitLogMessage(false, false, false);
            repository.UpdateHeadAndTerminalReference(lastCommit, logMessage);
            var result = default(IObjectRepository);
            if (_cherryPick.Repository.Container is ObjectRepositoryContainer container)
            {
                result = container.ReloadRepository(_cherryPick.Repository, lastCommit.Id);
            }
            return (CherryPickStatus.CherryPicked, result);
        }

        private void ComputeChanges(IRepository repository, Commit parentCommit)
        {
            var commit = repository.Lookup<Commit>(_cherryPick.CherryPickCommitId);

            using (var changes = repository.Diff.Compare<Patch>(
                parentCommit.Tree,
                commit.Tree))
            {
                foreach (var change in changes)
                {
                    switch (change.Status)
                    {
                        case ChangeKind.Modified:
                            var modified = RebaseProcessor.ComputeChanges_Modified(_cherryPick.Repository, _serializer, change,
                                relativePath => parentCommit[change.OldPath.GetSiblingFile(relativePath)]?.Target as Blob,
                                relativePath => commit[change.Path.GetSiblingFile(relativePath)]?.Target as Blob);
                            _cherryPick.ModifiedProperties.AddRange(modified);
                            break;
                        case ChangeKind.Added:
                            var added = RebaseProcessor.ComputeChanges_Added(_cherryPick.Repository, _serializer, repository, change,
                                relativePath => (commit[change.Path.GetSiblingFile(relativePath)]?.Target as Blob)?.GetContentText() ?? string.Empty);
                            if (added != null)
                            {
                                _cherryPick.AddedObjects.Add(added);
                            }
                            break;
                        case ChangeKind.Deleted:
                            var deleted = RebaseProcessor.ComputeChanges_Deleted(_serializer, repository, change,
                                relativePath => (commit[change.OldPath.GetSiblingFile(relativePath)]?.Target as Blob)?.GetContentText() ?? string.Empty);
                            if (deleted != null)
                            {
                                _cherryPick.DeletedObjects.Add(deleted);
                            }
                            break;
                        default:
                            throw new NotImplementedException($"Change type '{change.Status}' for branch merge is not supported.");
                    }
                }
            }
        }
    }
}
