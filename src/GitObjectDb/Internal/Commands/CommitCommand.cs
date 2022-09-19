using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Internal.Commands;

internal class CommitCommand : ICommitCommand
{
    private readonly ITreeValidation _treeValidation;

    public CommitCommand(ITreeValidation treeValidation)
    {
        _treeValidation = treeValidation;
    }

    public Commit Commit(IConnection connection,
                         TransformationComposer transformationComposer,
                         CommitDescription description,
                         Action<ITransformation>? beforeProcessing = null)
    {
        var tip = connection.Repository.Info.IsHeadUnborn ? null : connection.Repository.Head.Tip;
        var definition = transformationComposer.ApplyTransformations(connection.Repository.ObjectDatabase,
                                                                     tip,
                                                                     beforeProcessing);
        var parents = RetrieveParentsOfTheCommitBeingCreated(connection.Repository,
                                                             description.AmendPreviousCommit,
                                                             description.MergeParent).ToList();
        return Commit(connection,
                      definition,
                      description,
                      parents,
                      updateHead: true);
    }

    internal Commit Commit(IConnection connection,
                           Commit predecessor,
                           IEnumerable<ApplyUpdateTreeDefinition> transformations,
                           CommitDescription description,
                           bool updateHead = true,
                           Commit? mergeParent = null)
    {
        var modules = new ModuleCommands(predecessor.Tree);
        var definition = TreeDefinition.From(predecessor);
        foreach (var transformation in transformations)
        {
            transformation(predecessor.Tree, modules, connection.Repository.ObjectDatabase, definition);
        }
        var parents = new List<Commit> { predecessor };
        if (mergeParent != null)
        {
            parents.Add(mergeParent);
        }
        return Commit(connection,
                      definition,
                      description,
                      parents,
                      updateHead);
    }

    private Commit Commit(IConnection connection,
                          TreeDefinition definition,
                          CommitDescription description,
                          List<Commit> parents,
                          bool updateHead)
    {
        var tree = connection.Repository.ObjectDatabase.CreateTree(definition);
        _treeValidation.Validate(tree, connection.Model);
        var result = connection.Repository.ObjectDatabase.CreateCommit(
            description.Author, description.Committer, description.Message,
            tree,
            parents, false);
        if (updateHead)
        {
            var logMessage = result.BuildCommitLogMessage(description.AmendPreviousCommit,
                                                          connection.Repository.Info.IsHeadUnborn,
                                                          parents.Count > 1);
            connection.Repository.UpdateHeadAndTerminalReference(result, logMessage);
        }
        return result;
    }

    internal static List<Commit> RetrieveParentsOfTheCommitBeingCreated(IRepository repository,
                                                                        bool amendPreviousCommit,
                                                                        Commit? mergeParent = null)
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
            throw new NotSupportedException();
        }

        return parents;
    }
}
