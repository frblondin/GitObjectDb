using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Internal.Commands;

internal class CommitCommandUsingTree : ICommitCommand
{
    private readonly Func<ITreeValidation> _treeValidation;

    public CommitCommandUsingTree(Func<ITreeValidation> treeValidation)
    {
        _treeValidation = treeValidation;
    }

    public Commit Commit(TransformationComposer composer,
                         CommitDescription description,
                         Action<ITransformation>? beforeProcessing = null)
    {
        var branch = composer.Connection.Repository.Branches[composer.BranchName];
        var definition = ApplyTransformations(composer.Connection,
                                              composer.Transformations,
                                              branch?.Tip,
                                              beforeProcessing);
        var parents = RetrieveParentsOfTheCommitBeingCreated(composer.Connection.Repository,
                                                             branch,
                                                             description.AmendPreviousCommit,
                                                             description.MergeParent).ToList();
        return Commit(composer.Connection,
                      composer.BranchName,
                      definition,
                      description,
                      parents,
                      updateBranchTip: true);
    }

    public Commit Commit(IConnection connection,
                         string branchName,
                         IEnumerable<Delegate> transformations,
                         CommitDescription description,
                         Commit predecessor,
                         bool updateBranchTip = true)
    {
        var modules = new ModuleCommands(predecessor.Tree);
        var definition = TreeDefinition.From(predecessor);
        foreach (var transformation in transformations)
        {
            var action = (ApplyUpdateTreeDefinition)transformation;
            action.Invoke(predecessor.Tree,
                          modules,
                          connection.Serializer,
                          connection.Repository.ObjectDatabase,
                          definition);
        }
        var parents = new List<Commit> { predecessor };
        if (description.MergeParent is not null)
        {
            parents.Add(description.MergeParent);
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

    private static TreeDefinition ApplyTransformations(IConnection connection,
                                                       IEnumerable<ITransformation> transformations,
                                                       Commit? commit,
                                                       Action<ITransformation>? beforeProcessing = null)
    {
        var result = commit is not null ? TreeDefinition.From(commit) : new TreeDefinition();
        var modules = new ModuleCommands(commit?.Tree);
        var database = connection.Repository.ObjectDatabase;
        foreach (var transformation in transformations.Cast<ITransformationInternal>())
        {
            beforeProcessing?.Invoke(transformation);
            var action = (ApplyUpdateTreeDefinition)transformation.Action;
            action.Invoke(commit?.Tree, modules, connection.Serializer, database, result);
        }

        if (modules.HasAnyChange)
        {
            using var stream = modules.CreateStream();
            var blob = database.CreateBlob(stream);
            result.Add(ModuleCommands.ModuleFile, blob, Mode.NonExecutableFile);
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
