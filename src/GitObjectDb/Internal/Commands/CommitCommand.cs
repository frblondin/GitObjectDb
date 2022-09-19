using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Internal.Commands
{
    internal partial class CommitCommand : ICommitCommand
    {
        private readonly ITreeValidation _treeValidation;

        public CommitCommand(ITreeValidation treeValidation)
        {
            _treeValidation = treeValidation;
        }

        public Commit Commit(IConnectionInternal connection, TransformationComposer transformationComposer, string message, Signature author, Signature committer, bool amendPreviousCommit = false, Commit? mergeParent = null, Action<ITransformation>? beforeProcessing = null)
        {
            var tip = connection.Info.IsHeadUnborn ? null : connection.Head.Tip;
            var definition = transformationComposer.ApplyTransformations(connection.Repository.ObjectDatabase, tip, beforeProcessing);
            var parents = RetrieveParentsOfTheCommitBeingCreated(connection.Repository, amendPreviousCommit, mergeParent).ToList();
            return Commit(connection, definition, message, author, committer, parents, amendPreviousCommit, updateHead: true);
        }

        internal Commit Commit(IConnectionInternal connection, Commit predecessor, IEnumerable<ApplyUpdateTreeDefinition> transformations, string message, Signature author, Signature committer, bool amendPreviousCommit = false, bool updateHead = true, Commit? mergeParent = null)
        {
            var definition = TreeDefinition.From(predecessor);
            foreach (var transformation in transformations)
            {
                transformation(connection.Repository.ObjectDatabase, definition, predecessor.Tree);
            }
            var parents = new List<Commit> { predecessor };
            if (mergeParent != null)
            {
                parents.Add(mergeParent);
            }
            return Commit(connection, definition, message, author, committer, parents, amendPreviousCommit, updateHead);
        }

        private Commit Commit(IConnectionInternal connection, TreeDefinition definition, string message, Signature author, Signature committer, List<Commit> parents, bool amendPreviousCommit, bool updateHead)
        {
            var tree = connection.Repository.ObjectDatabase.CreateTree(definition);
            _treeValidation.Validate(tree, connection.Model);
            var result = connection.Repository.ObjectDatabase.CreateCommit(
                author, committer, message,
                tree,
                parents, false);
            if (updateHead)
            {
                var logMessage = result.BuildCommitLogMessage(amendPreviousCommit, connection.Info.IsHeadUnborn, parents.Count > 1);
                connection.Repository.UpdateHeadAndTerminalReference(result, logMessage);
            }
            return result;
        }

        internal static List<Commit> RetrieveParentsOfTheCommitBeingCreated(IRepository repository, bool amendPreviousCommit, Commit? mergeParent = null)
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
