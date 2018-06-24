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
    public static class RepositoryExtensions
    {
        public static Blob CreateBlob(this Repository repository, StringBuilder content)
        {
            using (var stream = new StringBuilderStream(content))
            {
                return repository.ObjectDatabase.CreateBlob(stream);
            }
        }

        public static Commit Commit(this Repository repository, Action<Repository, TreeDefinition> actions, string message, Signature author, Signature committer, CommitOptions options = null)
        {
            var treeDefinition = !repository.Info.IsHeadUnborn ? TreeDefinition.From(repository.Head.Tip.Tree) : new TreeDefinition();
            actions(repository, treeDefinition);
            return Commit(repository, treeDefinition, message, author, committer, options);
        }

        public static Commit Commit(this Repository repository, TreeDefinition treeDefinition, string message, Signature author, Signature committer, CommitOptions options = null)
        {
            if (options == null) options = new CommitOptions();

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

            while (true) //TODO: Implement max nesting level
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
