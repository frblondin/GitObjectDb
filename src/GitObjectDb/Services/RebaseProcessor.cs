using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Merge;
using GitObjectDb.Models.Rebase;
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
    /// Applies the rebase changes.
    /// </summary>
    internal class RebaseProcessor
    {
        private readonly ObjectRepositoryRebase _rebase;

        private readonly ComputeTreeChangesFactory _computeTreeChangesFactory;
        private readonly IObjectRepositorySerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RebaseProcessor"/> class.
        /// </summary>
        /// <param name="objectRepositoryRebase">The object repository rebase.</param>
        /// <param name="computeTreeChangesFactory">The <see cref="IComputeTreeChanges"/> factory.</param>
        /// <param name="serializerFactory">The <see cref="ObjectRepositorySerializerFactory"/> factory.</param>
        [ActivatorUtilitiesConstructor]
        internal RebaseProcessor(ObjectRepositoryRebase objectRepositoryRebase,
            ComputeTreeChangesFactory computeTreeChangesFactory, ObjectRepositorySerializerFactory serializerFactory)
        {
            if (serializerFactory == null)
            {
                throw new ArgumentNullException(nameof(serializerFactory));
            }

            _rebase = objectRepositoryRebase ?? throw new ArgumentNullException(nameof(objectRepositoryRebase));

            _computeTreeChangesFactory = computeTreeChangesFactory ?? throw new ArgumentNullException(nameof(computeTreeChangesFactory));
            _serializer = serializerFactory(new ModelObjectSerializationContext(objectRepositoryRebase.Repository.Container));
        }

        /// <summary>
        /// Creates a new instance of <see cref="RebaseProcessor"/>.
        /// </summary>
        /// <param name="objectRepositoryRebase">The object repository rebase.</param>
        /// <returns>The newly created instance.</returns>
        internal delegate RebaseProcessor Factory(ObjectRepositoryRebase objectRepositoryRebase);

        private IObjectRepository CurrentTransformedRepository => _rebase.Transformations.LastOrDefault() ?? _rebase.StartRepository;

        /// <summary>
        /// Continues the rebase operation.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <returns>The new <see cref="RebaseStatus"/>.</returns>
        internal RebaseStatus Continue(IRepository repository)
        {
            if (_rebase.CompletedStepCount == _rebase.TotalStepCount)
            {
                throw new GitObjectDbException("The rebase process has completed.");
            }
            if (_rebase.ModifiedChunks.Any(c => c.IsInConflict))
            {
                throw new GitObjectDbException("There are remaining unresolved conflicts.");
            }
            return CompleteStep(repository);
        }

        /// <summary>
        /// Continues the next rebase operation.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <returns>The new <see cref="RebaseStatus"/>.</returns>
        internal RebaseStatus ContinueNext(IRepository repository)
        {
            ComputeChanges(repository);

            if (_rebase.ModifiedChunks.Any(c => c.IsInConflict))
            {
                return RebaseStatus.Conflicts;
            }
            else
            {
                return CompleteStep(repository);
            }
        }

        private RebaseStatus CompleteStep(IRepository repository)
        {
            var transformations = new TransformationFromChunkChanges(_rebase.ModifiedChunks, _rebase.AddedObjects, _rebase.DeletedObjects);
            var transformed = CurrentTransformedRepository.DataAccessor.With(CurrentTransformedRepository, transformations);
            transformed.SetRepositoryData(CurrentTransformedRepository.RepositoryDescription, ObjectId.Zero);
            _rebase.Transformations.Add(transformed);

            _rebase.ClearChanges();
            _rebase.CompletedStepCount++;

            if (_rebase.CompletedStepCount != _rebase.TotalStepCount)
            {
                return ContinueNext(repository);
            }
            else
            {
                return CompleteRebase(repository);
            }
        }

        private RebaseStatus CompleteRebase(IRepository r)
        {
            var computeChanges = _computeTreeChangesFactory(_rebase.Repository.Container, _rebase.Repository.RepositoryDescription);
            var previous = _rebase.StartRepository;
            var lastCommit = r.Lookup<Commit>(_rebase.RebaseCommitId);
            foreach (var info in _rebase.Transformations.Zip(_rebase.ReplayedCommits, (repository, commit) => (repository, r.Lookup<Commit>(commit))))
            {
                var changes = computeChanges.Compare(previous, info.repository);
                if (changes.Any())
                {
                    var definition = TreeDefinition.From(lastCommit);
                    r.UpdateTreeDefinition(changes, definition, _serializer);
                    var tree = r.ObjectDatabase.CreateTree(definition);
                    previous = info.repository;
                    lastCommit = r.ObjectDatabase.CreateCommit(info.Item2.Author, info.Item2.Committer, info.Item2.Message, tree, new[] { lastCommit }, false);
                }
            }
            var logMessage = lastCommit.BuildCommitLogMessage(false, false, false);
            r.UpdateHeadAndTerminalReference(lastCommit, logMessage);
            if (_rebase.Repository.Container is ObjectRepositoryContainer container)
            {
                container.ReloadRepository(_rebase.Repository, lastCommit.Id);
            }
            return RebaseStatus.Complete;
        }

        private void ComputeChanges(IRepository repository)
        {
            _rebase.ClearChanges();
            var previousCommit = _rebase.CompletedStepCount == 0 ? _rebase.MergeBaseCommitId : _rebase.ReplayedCommits[_rebase.CompletedStepCount];
            var commit = _rebase.ReplayedCommits[_rebase.CompletedStepCount];
            using (var changes = repository.Diff.Compare<Patch>(
                repository.Lookup<Commit>(previousCommit).Tree,
                repository.Lookup<Commit>(commit).Tree))
            {
                foreach (var change in changes)
                {
                    switch (change.Status)
                    {
                        case ChangeKind.Modified:
                            ComputeChanges_Modified(repository, change);
                            break;
                        case ChangeKind.Added:
                            ComputeChanges_Added(repository, change);
                            break;
                        case ChangeKind.Deleted:
                            ComputeChanges_Deleted(repository, change);
                            break;
                        default:
                            throw new NotImplementedException($"Change type '{change.Status}' for branch merge is not supported.");
                    }
                }
            }
        }

        private void ComputeChanges_Modified(IRepository repository, PatchEntryChanges change)
        {
            var currentObject = CurrentTransformedRepository.TryGetFromGitPath(change.Path) ??
                throw new NotImplementedException($"Conflict as a modified node {change.Path} has been deleted in current rebase state.");
            var changeStart = _serializer.Deserialize(repository.Lookup<Blob>(change.OldOid).GetContentStream());
            var changeEnd = _serializer.Deserialize(repository.Lookup<Blob>(change.Oid).GetContentStream());
            var changes = ObjectRepositoryMerge.ComputeModifiedChunks(change, changeStart, changeEnd, currentObject);

            foreach (var modifiedProperty in changes)
            {
                if (typeof(IObjectRepositoryIndex).IsAssignableFrom(modifiedProperty.Property.Property.ReflectedType))
                {
                    // Indexes will be recomputed anyways from the changes when committed,
                    // so there is no need to track them in the modified chunks
                    continue;
                }

                _rebase.ModifiedChunks.Add(modifiedProperty);
            }
        }

        private void ComputeChanges_Added(IRepository repository, PatchEntryChanges change)
        {
            if (CurrentTransformedRepository.TryGetFromGitPath(change.Path) != null)
            {
                throw new NotImplementedException("Node already present in current state.");
            }
            var parentDataPath = change.Path.GetDataParentDataPath();
            if (CurrentTransformedRepository.TryGetFromGitPath(parentDataPath) == null)
            {
                throw new NotImplementedException("Node addition while parent has been deleted in head is not supported.");
            }

            var @new = _serializer.Deserialize(repository.Lookup<Blob>(change.Oid).GetContentStream());
            var parentId = change.Path.GetDataParentId(_rebase.Repository);
            _rebase.AddedObjects.Add(new ObjectRepositoryAdd(change.Path, @new, parentId));
        }

        private void ComputeChanges_Deleted(IRepository repository, PatchEntryChanges change)
        {
            var folder = change.Path.Replace($"/{FileSystemStorage.DataFile}", string.Empty);
            if (_rebase.ModifiedUpstreamBranchEntries.Any(c =>
                c.Path.Equals(folder, StringComparison.OrdinalIgnoreCase) &&
                (c.Status == ChangeKind.Added || c.Status == ChangeKind.Modified)))
            {
                throw new NotImplementedException("Node deletion while children have been added or modified in head is not supported.");
            }

            var mergeBaseObject = _serializer.Deserialize(repository.Lookup<Blob>(change.OldOid).GetContentStream());
            _rebase.DeletedObjects.Add(new ObjectRepositoryDelete(change.Path, mergeBaseObject.Id));
        }
    }
}
