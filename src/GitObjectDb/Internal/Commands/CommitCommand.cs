using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Internal.Commands;

internal class CommitCommand : ICommitCommand
{
    private readonly Func<ITreeValidation> _treeValidation;

    public CommitCommand(Func<ITreeValidation> treeValidation)
    {
        _treeValidation = treeValidation;
    }

    public Commit Commit(IConnection connection,
                         TransformationComposer transformationComposer,
                         CommitDescription description,
                         Action<ITransformation>? beforeProcessing = null)
    {
        var branch = connection.Repository.Branches[transformationComposer.BranchName];
        var definition = transformationComposer.ApplyTransformations(connection.Repository.ObjectDatabase,
                                                                     branch?.Tip,
                                                                     beforeProcessing);
        var parents = RetrieveParentsOfTheCommitBeingCreated(connection.Repository,
                                                             branch,
                                                             description.AmendPreviousCommit,
                                                             description.MergeParent).ToList();
        return Commit(connection,
                      transformationComposer.BranchName,
                      definition,
                      description,
                      parents,
                      updateBranchTip: true);
    }

    internal Commit Commit(IConnection connection,
                           string branchName,
                           Commit predecessor,
                           IEnumerable<ApplyUpdateTreeDefinition> transformations,
                           CommitDescription description,
                           bool updateBranchTip = true,
                           Commit? mergeParent = null)
    {
        var modules = new ModuleCommands(predecessor.Tree);
        var definition = TreeDefinition.From(predecessor);
        foreach (var transformation in transformations)
        {
            transformation(predecessor.Tree,
                           modules,
                           connection.Serializer,
                           connection.Repository.ObjectDatabase,
                           definition);
        }
        var parents = new List<Commit> { predecessor };
        if (mergeParent != null)
        {
            parents.Add(mergeParent);
        }
        return Commit(connection,
                      branchName,
                      definition,
                      description,
                      parents,
                      updateBranchTip);
    }

    private Commit Commit(IConnection connection,
                          string branchName,
                          TreeDefinition definition,
                          CommitDescription description,
                          List<Commit> parents,
                          bool updateBranchTip)
    {
        var tree = connection.Repository.ObjectDatabase.CreateTree(definition);
        var validation = _treeValidation.Invoke();
        validation.Validate(tree, connection.Model, connection.Serializer);
        var result = connection.Repository.ObjectDatabase.CreateCommit(
            description.Author, description.Committer, description.Message,
            tree,
            parents, false);
        if (updateBranchTip)
        {
            var logMessage = result.BuildCommitLogMessage(description.AmendPreviousCommit,
                                                          parents.Count > 1);
            var reference = connection.Repository.Branches[branchName]?.Reference ??
                connection.Repository.Refs.UpdateTarget("HEAD", $"refs/heads/{branchName}");
            connection.Repository.UpdateBranchTip(reference, result, logMessage);
        }
        return result;
    }

    internal static List<Commit> RetrieveParentsOfTheCommitBeingCreated(IRepository repository,
                                                                        Branch? branch,
                                                                        bool amendPreviousCommit,
                                                                        Commit? mergeParent = null)
    {
        if (amendPreviousCommit)
        {
            if (branch is null)
            {
                throw new GitObjectDbNonExistingBranchException();
            }
            return branch.Tip.Parents.ToList();
        }

        var parents = new List<Commit>();
        if (branch?.Tip is not null)
        {
            parents.Add(branch.Tip);
        }

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
