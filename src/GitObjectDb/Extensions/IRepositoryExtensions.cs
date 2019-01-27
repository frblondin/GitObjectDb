using GitObjectDb;
using GitObjectDb.Git.Hooks;
using GitObjectDb.IO;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LibGit2Sharp
{
    /// <summary>
    /// A set of methods for instances of <see cref="IRepository"/>.
    /// </summary>
    internal static partial class IRepositoryExtensions
    {
        /// <summary>
        /// Creates the Blob from a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="content">The content.</param>
        /// <returns>New newly created Blob.</returns>
        internal static Blob CreateBlob(this IRepository repository, StringBuilder content)
        {
            using (var stream = new StringBuilderStream(content))
            {
                return repository.ObjectDatabase.CreateBlob(stream);
            }
        }

        /// <summary>
        /// Creates the Blob from a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="content">The content.</param>
        /// <returns>New newly created Blob.</returns>
        internal static Blob CreateBlob(this IRepository repository, string content)
        {
            using (var stream = new MemoryStream(Encoding.Default.GetBytes(content)))
            {
                return repository.ObjectDatabase.CreateBlob(stream);
            }
        }

        internal static Commit CommitChanges(this IRepository repository, ObjectRepositoryChangeCollection changes, IObjectRepositorySerializer serializer, string message, Signature author, Signature committer, GitHooks hooks, CommitOptions options = null, Commit mergeParent = null)
        {
            TreeDefinition definition;
            if (changes.OldRepository?.CommitId != null)
            {
                if (repository.Head.Tip.Id != changes.OldRepository.CommitId)
                {
                    throw new GitObjectDbException("Changes are not based on the HEAD commit.");
                }
                var startCommit = repository.Lookup<Commit>(changes.OldRepository.CommitId);
                definition = TreeDefinition.From(startCommit);
            }
            else if (repository.Info.IsHeadUnborn)
            {
                definition = new TreeDefinition();
            }
            else
            {
                throw new GitObjectDbException("Changes are not based on the HEAD commit.");
            }

            if (!hooks.OnCommitStarted(changes, message))
            {
                return null;
            }

            repository.UpdateTreeDefinition(changes, definition, serializer);

            var result = Commit(repository, definition, message, author, committer, options, mergeParent);
            hooks.OnCommitCompleted(changes, message, result.Id);

            return result;
        }

        internal static void UpdateTreeDefinition(this IRepository repository, ObjectRepositoryChangeCollection changes, TreeDefinition definition, IObjectRepositorySerializer serializer)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            UpdateChangeTreeDefinitions(repository, changes.Modified, definition, serializer);
            UpdateChangeTreeDefinitions(repository, changes.Added, definition, serializer);
            UpdateDeletionTreeDefinitions(changes.Deleted, definition);
            UpdateIndexTreeDefinitions(repository, changes, definition, serializer);
        }

        private static void UpdateChangeTreeDefinitions(IRepository repository, IEnumerable<ObjectRepositoryEntryChanges> changes, TreeDefinition definition, IObjectRepositorySerializer serializer)
        {
            var buffer = new StringBuilder();
            foreach (var change in changes)
            {
                if (change.New is IObjectRepositoryIndex index)
                {
                    // Index are managed separately
                    continue;
                }
                buffer.Clear();
                serializer.Serialize(change.New, buffer);
                definition.Add(change.Path, repository.CreateBlob(buffer), Mode.NonExecutableFile);
            }
        }

        private static void UpdateDeletionTreeDefinitions(IEnumerable<ObjectRepositoryEntryChanges> deletions, TreeDefinition definition)
        {
            foreach (var deleted in deletions)
            {
                definition.Remove(deleted.Path);
            }
        }

        private static void UpdateIndexTreeDefinitions(IRepository repository, ObjectRepositoryChangeCollection changes, TreeDefinition definition, IObjectRepositorySerializer serializer)
        {
            var buffer = new StringBuilder();
            foreach (var index in changes.NewRepository.Indexes)
            {
                var fullScan = changes.Added.Any(c => c.New.Id == index.Id);
                if (UpdateAndSerializerIndex(index, changes, serializer, buffer, fullScan))
                {
                    definition.Add(index.GetDataPath(), repository.CreateBlob(buffer), Mode.NonExecutableFile);
                }
            }
        }

        private static bool UpdateAndSerializerIndex(IObjectRepositoryIndex index, ObjectRepositoryChangeCollection changes, IObjectRepositorySerializer serializer, StringBuilder buffer, bool fullScan)
        {
            buffer.Clear();
            var binding = index.DataAccessor.ConstructorParameterBinding;
            var updatedIndex = fullScan ? index.FullScan() : index.Update(changes);
            if (updatedIndex == null)
            {
                return false;
            }

            var cloned = (IObjectRepositoryIndex)binding.Cloner(index,
                (instance, propertyName, type, fallback) =>
                    propertyName == nameof(IObjectRepositoryIndex.Values) ? updatedIndex : fallback,
                (childProperty, children, @new, dataAccessor) =>
                    throw new NotSupportedException("Index should not contain child properties."));
            serializer.Serialize(cloned, buffer);
            return true;
        }

        /// <summary>
        /// Inserts a <see cref="LibGit2Sharp.Commit" /> into the object database by applying a <see cref="TreeDefinition"/>.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="definition">The tree definition.</param>
        /// <param name="message">The message.</param>
        /// <param name="author">The author.</param>
        /// <param name="committer">The committer.</param>
        /// <param name="options">The options.</param>
        /// <param name="mergeParent">The parent commit for a merge.</param>
        /// <returns>The created <see cref="LibGit2Sharp.Commit" />.</returns>
        internal static Commit Commit(this IRepository repository, TreeDefinition definition, string message, Signature author, Signature committer, CommitOptions options = null, Commit mergeParent = null)
        {
            if (options == null)
            {
                options = new CommitOptions();
            }

            var parents = RetrieveParentsOfTheCommitBeingCreated(repository, options.AmendPreviousCommit, mergeParent).ToList();
            var tree = repository.ObjectDatabase.CreateTree(definition);
            var commit = repository.ObjectDatabase.CreateCommit(author, committer, message, tree, parents, false);
            var logMessage = commit.BuildCommitLogMessage(options.AmendPreviousCommit, repository.Info.IsHeadUnborn, parents.Count > 1);
            UpdateHeadAndTerminalReference(repository, commit, logMessage);
            return commit;
        }

        private static IEnumerable<Commit> RetrieveParentsOfTheCommitBeingCreated(IRepository repository, bool amendPreviousCommit, Commit mergeParent = null)
        {
            if (amendPreviousCommit)
            {
                return repository.Head.Tip.Parents;
            }

            if (repository.Info.IsHeadUnborn)
            {
                return Enumerable.Empty<Commit>();
            }

            var parents = new List<Commit> { repository.Head.Tip };
            if (mergeParent != null)
            {
                parents.Add(mergeParent);
            }

            if (repository.Info.CurrentOperation == CurrentOperation.Merge)
            {
                throw new NotImplementedException();
            }

            return parents;
        }

        /// <summary>
        /// Builds the commit log message.
        /// </summary>
        /// <param name="commit">The commit.</param>
        /// <param name="amendPreviousCommit">if set to <c>true</c> [amend previous commit].</param>
        /// <param name="isHeadOrphaned">if set to <c>true</c> [is head orphaned].</param>
        /// <param name="isMergeCommit">if set to <c>true</c> [is merge commit].</param>
        /// <returns>The commit log message.</returns>
        internal static string BuildCommitLogMessage(this Commit commit, bool amendPreviousCommit, bool isHeadOrphaned, bool isMergeCommit)
        {
            var kind = string.Empty;
            if (isHeadOrphaned)
            {
                kind = " (initial)";
            }
            else if (amendPreviousCommit)
            {
                kind = " (amend)";
            }
            else if (isMergeCommit)
            {
                kind = " (merge)";
            }

            return $"commit{kind}: {commit.MessageShort}";
        }

        /// <summary>
        /// Updates the head and terminal reference.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="commit">The commit.</param>
        /// <param name="reflogMessage">The reflog message.</param>
        internal static void UpdateHeadAndTerminalReference(this IRepository repository, Commit commit, string reflogMessage)
        {
            var reference = repository.Refs.Head;

            while (true)
            {
                switch (reference)
                {
                    case DirectReference direct:
                        repository.Refs.UpdateTarget(direct, commit.Id, reflogMessage);
                        return;
                    case SymbolicReference symRef:
                        reference = symRef.Target;
                        if (reference == null)
                        {
                            repository.Refs.Add(symRef.TargetIdentifier, commit.Id, reflogMessage);
                            return;
                        }
                        break;
                    default:
                        throw new NotSupportedException($"The reference type {reference?.GetType().ToString() ?? "null"} is not supported.");
                }
            }
        }
    }
}