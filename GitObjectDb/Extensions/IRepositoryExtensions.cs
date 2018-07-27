using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.IO;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        /// <summary>
        /// Inserts a <see cref="LibGit2Sharp.Commit" /> into the object database by applying a <see cref="TreeDefinition"/>.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="changes">The changes.</param>
        /// <param name="message">The message.</param>
        /// <param name="author">The author.</param>
        /// <param name="committer">The committer.</param>
        /// <param name="hooks">The hooks.</param>
        /// <param name="options">The options.</param>
        /// <param name="mergeParent">The parent commit for a merge.</param>
        /// <returns>The created <see cref="LibGit2Sharp.Commit" />.</returns>
        internal static Commit CommitChanges(this IRepository repository, MetadataTreeChanges changes, string message, Signature author, Signature committer, GitHooks hooks, CommitOptions options = null, Commit mergeParent = null)
        {
            TreeDefinition definition;
            if (changes.OldRepository?.CommitId != null)
            {
                if (repository.Head.Tip.Id != changes.OldRepository.CommitId)
                {
                    throw new NotSupportedException("Changes are not based on the HEAD commit.");
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
                throw new NotSupportedException("Changes are not based on the HEAD commit.");
            }

            if (!hooks.OnCommitStarted(changes, message))
            {
                return null;
            }

            changes.UpdateTreeDefinition(repository, definition);

            var result = Commit(repository, definition, message, author, committer, options, mergeParent);
            hooks.OnCommitCompleted(changes, message, result.Id);

            return result;
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
            var logMessage = BuildCommitLogMessage(commit, options.AmendPreviousCommit, repository.Info.IsHeadUnborn, parents.Count > 1);
            UpdateHeadAndTerminalReference(repository, commit, logMessage);
            return commit;
        }

        static IEnumerable<Commit> RetrieveParentsOfTheCommitBeingCreated(IRepository repository, bool amendPreviousCommit, Commit mergeParent = null)
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
                if (reference is DirectReference)
                {
                    repository.Refs.UpdateTarget(reference, commit.Id, reflogMessage);
                    return;
                }

                var symRef = (SymbolicReference)reference;

                reference = symRef.Target;

                if (reference == null)
                {
                    repository.Refs.Add(symRef.TargetIdentifier, commit.Id, reflogMessage);
                    return;
                }
            }
        }
    }
}