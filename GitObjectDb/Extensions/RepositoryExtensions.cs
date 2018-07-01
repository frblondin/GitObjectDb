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
    /// A set of methods for instances of <see cref="Repository"/>.
    /// </summary>
    internal static class RepositoryExtensions
    {
        /// <summary>
        /// Creates the Blob from a <see cref="StringBuilder"/>.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="content">The content.</param>
        /// <returns>New newly created Blob.</returns>
        internal static Blob CreateBlob(this Repository repository, StringBuilder content)
        {
            using (var stream = new StringBuilderStream(content))
            {
                return repository.ObjectDatabase.CreateBlob(stream);
            }
        }

        /// <summary>
        /// Inserts a <see cref="LibGit2Sharp.Commit" /> into the object database by applying actions to a <see cref="TreeDefinition"/>.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="actions">The actions to be applied to the <see cref="TreeDefinition"/>.</param>
        /// <param name="message">The message of the commit.</param>
        /// <param name="author">The author.</param>
        /// <param name="committer">The committer.</param>
        /// <param name="options">The options.</param>
        /// <returns>The created <see cref="LibGit2Sharp.Commit" />.</returns>
        internal static Commit Commit(this Repository repository, Action<Repository, TreeDefinition> actions, string message, Signature author, Signature committer, CommitOptions options = null)
        {
            var treeDefinition = !repository.Info.IsHeadUnborn ? TreeDefinition.From(repository.Head.Tip.Tree) : new TreeDefinition();
            actions(repository, treeDefinition);
            return Commit(repository, treeDefinition, message, author, committer, options);
        }

        /// <summary>
        /// Inserts a <see cref="LibGit2Sharp.Commit" /> into the object database by applying a <see cref="TreeDefinition"/>.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="treeDefinition">The tree definition.</param>
        /// <param name="message">The message.</param>
        /// <param name="author">The author.</param>
        /// <param name="committer">The committer.</param>
        /// <param name="options">The options.</param>
        /// <returns>The created <see cref="LibGit2Sharp.Commit" />.</returns>
        internal static Commit Commit(this Repository repository, TreeDefinition treeDefinition, string message, Signature author, Signature committer, CommitOptions options = null)
        {
            if (options == null)
            {
                options = new CommitOptions();
            }

            var parents = RetrieveParentsOfTheCommitBeingCreated(repository, options.AmendPreviousCommit).ToList();
            var tree = repository.ObjectDatabase.CreateTree(treeDefinition);
            var commit = repository.ObjectDatabase.CreateCommit(author, committer, message, tree, parents, false);
            var logMessage = BuildCommitLogMessage(commit, options.AmendPreviousCommit, repository.Info.IsHeadUnborn, parents.Count > 1);
            UpdateHeadAndTerminalReference(repository, commit, logMessage);
            return commit;
        }

        static IEnumerable<Commit> RetrieveParentsOfTheCommitBeingCreated(Repository repository, bool amendPreviousCommit)
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

            if (repository.Info.CurrentOperation == CurrentOperation.Merge)
            {
                throw new NotImplementedException();
            }

            return parents;
        }

        static string BuildCommitLogMessage(Commit commit, bool amendPreviousCommit, bool isHeadOrphaned, bool isMergeCommit)
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

        static void UpdateHeadAndTerminalReference(Repository repository, Commit commit, string reflogMessage)
        {
            var reference = repository.Refs.Head;

            // TODO: Implement max nesting level
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
