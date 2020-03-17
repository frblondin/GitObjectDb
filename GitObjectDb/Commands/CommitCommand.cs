using GitObjectDb.Validations;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Commands
{
    internal class CommitCommand
    {
        private readonly ITreeValidation _treeValidation;

        public CommitCommand(ITreeValidation treeValidation)
        {
            _treeValidation = treeValidation;
        }

        internal Commit Commit(Repository repository, IEnumerable<ApplyUpdateTreeDefinition> transformations, string message, Signature author, Signature committer, bool amendPreviousCommit = false, Commit mergeParent = null)
        {
            var tip = repository.Info.IsHeadUnborn ? null : repository.Head.Tip;
            var definition = tip != null ? TreeDefinition.From(tip) : new TreeDefinition();
            foreach (var transformation in transformations)
            {
                transformation(repository.ObjectDatabase, definition, tip?.Tree);
            }
            var parents = RetrieveParentsOfTheCommitBeingCreated(repository, amendPreviousCommit, mergeParent).ToList();
            return Commit(repository, definition, message, author, committer, parents, amendPreviousCommit, true);
        }

        internal Commit Commit(Repository repository, Commit predecessor, IEnumerable<ApplyUpdateTreeDefinition> transformations, string message, Signature author, Signature committer, bool amendPreviousCommit = false, bool updateHead = true, Commit mergeParent = null)
        {
            var definition = TreeDefinition.From(predecessor);
            foreach (var transformation in transformations)
            {
                transformation(repository.ObjectDatabase, definition, predecessor.Tree);
            }
            var parents = new List<Commit> { predecessor };
            if (mergeParent != null)
            {
                parents.Add(mergeParent);
            }
            return Commit(repository, definition, message, author, committer, parents, amendPreviousCommit, updateHead);
        }

        private Commit Commit(Repository repository, TreeDefinition definition, string message, Signature author, Signature committer, List<Commit> parents, bool amendPreviousCommit, bool updateHead)
        {
            var tree = repository.ObjectDatabase.CreateTree(definition);
            _treeValidation.Validate(tree);
            var result = repository.ObjectDatabase.CreateCommit(
                author, committer, message,
                tree,
                parents, false);
            if (updateHead)
            {
                var logMessage = result.BuildCommitLogMessage(amendPreviousCommit, repository.Info.IsHeadUnborn, parents.Count > 1);
                repository.UpdateHeadAndTerminalReference(result, logMessage);
            }
            return result;
        }

        private List<Commit> RetrieveParentsOfTheCommitBeingCreated(Repository repository, bool amendPreviousCommit, LibGit2Sharp.Commit mergeParent = null)
        {
            if (amendPreviousCommit)
            {
                return repository.Head.Tip.Parents.ToList();
            }

            var parents = new List<Commit>();
            if (repository.Info.IsHeadUnborn)
            {
                return parents;
            }

            parents.Add(repository.Head.Tip);
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
    }
}
