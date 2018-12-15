using System;
using System.Collections.Generic;
using System.Text;
using GitObjectDb.Models;
using GitObjectDb.Services;
using LibGit2Sharp;

namespace Microsoft.Extensions.Logging
{
    internal static class ILoggerExtensions
    {
        #region ObjectRepositoryContainer
        private static readonly Action<ILogger, string, Exception> _containerCreated = LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1, nameof(ContainerCreated)),
            "Container created using path '{Path}'.");
        internal static void ContainerCreated(this ILogger<ObjectRepositoryContainer> logger, string path) =>
            _containerCreated(logger, path, null);

        private static readonly Action<ILogger, UniqueId, ObjectId, Exception> _repositoryAdded = LoggerMessage.Define<UniqueId, ObjectId>(
            LogLevel.Debug,
            new EventId(2, nameof(RepositoryAdded)),
            "Container created using path '{Id}' at commit id {CommitId}.");
        internal static void RepositoryAdded(this ILogger<ObjectRepositoryContainer> logger, UniqueId id, ObjectId commitId) =>
            _repositoryAdded(logger, id, commitId, null);

        private static readonly Action<ILogger, UniqueId, ObjectId, Exception> _repositoryUpdated = LoggerMessage.Define<UniqueId, ObjectId>(
            LogLevel.Debug,
            new EventId(3, nameof(RepositoryUpdated)),
            "Container updated using path '{Id}' at commit id {CommitId}.");
        internal static void RepositoryUpdated(this ILogger<ObjectRepositoryContainer> logger, UniqueId id, ObjectId commitId) =>
            _repositoryUpdated(logger, id, commitId, null);
        #endregion

        #region ComputeTreeChanges
        private static readonly Action<ILogger, int, int, int, ObjectId, ObjectId, Exception> _changesComputed = LoggerMessage.Define<int, int, int, ObjectId, ObjectId>(
            LogLevel.Debug,
            new EventId(100, nameof(ChangesComputed)),
            "{Modified} modifications, {Added} additions, and {Deleted} deletions detected between {Old} and {New}.");
        internal static void ChangesComputed(this ILogger<ComputeTreeChanges> logger, int modified, int added, int deleted, ObjectId old, ObjectId @new) =>
            _changesComputed(logger, modified, added, deleted, old, @new, null);
        #endregion
    }
}
