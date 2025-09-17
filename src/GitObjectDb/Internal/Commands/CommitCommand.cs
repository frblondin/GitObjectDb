using GitDotNet;
using GitObjectDb.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Internal.Commands;

internal class CommitCommand : ICommitCommand
{
    private readonly Func<ITreeValidation> _treeValidation;

    public CommitCommand(Func<ITreeValidation> treeValidation)
    {
        _treeValidation = treeValidation;

        GitCliCommand.ThrowIfGitNotInstalled();
    }

    public async Task<CommitEntry> CommitAsync(ChangeComposer composer,
        CommitDescription description,
        Action<ITransformation>? beforeProcessing = null)
    {
        var parents = composer.Connection.Repository.Branches.TryGet(composer.BranchName, out var branch) ?
            await RetrieveParentsOfTheCommitBeingCreatedAsync(
                composer.Connection.Repository,
                branch,
                description.AmendPreviousCommit,
                description.MergeParent).ConfigureAwait(false) :
            [];
        return await CommitAsync(composer.Connection,
            async info => await ApplyTransformationsAsync(composer.Connection,
                composer.Transformations.Values,
                branch != null ? await branch.GetTipAsync().ConfigureAwait(false) : null,
                info,
                beforeProcessing).ConfigureAwait(false),
            composer.BranchName,
            parents,
            description);
    }

    public async Task<CommitEntry> CommitAsync(IConnection connection,
        string branchName,
        IEnumerable<ApplyUpdate> transformations,
        CommitDescription description,
        CommitEntry predecessor)
    {
        var tree = await predecessor.GetRootTreeAsync().ConfigureAwait(false);
        var modules = await ModuleCommands.GetAsync(tree).ConfigureAwait(false);
        var parents = GetParents(description, predecessor);
        return await CommitAsync(connection,
            async composer =>
            {
                foreach (var transformation in transformations)
                {
                    await transformation.Invoke(tree, modules, connection.Serializer, composer).ConfigureAwait(false);
                }
                return composer;
            },
            branchName,
            parents,
            description).ConfigureAwait(false);
    }

    public async Task<CommitEntry> CommitAsync(IConnection connection,
        Func<ITransformationComposer, Task<ITransformationComposer>> transform,
        string branchName,
        List<CommitEntry> parents,
        CommitDescription description)
    {
        var commit = await connection.Repository.CommitAsync(branchName, transform,
            connection.Repository.CreateCommit(description.Message, parents, description.Author, description.Committer),
            new(UpdateBranch: false)).ConfigureAwait(false);
        return await ValidateAndUpdateBranchTipAsync(connection, branchName, commit).ConfigureAwait(false);
    }

    internal static List<CommitEntry> GetParents(CommitDescription description, CommitEntry predecessor)
    {
        var parents = new List<CommitEntry> { predecessor };
        if (description.MergeParent is not null)
        {
            parents.Add(description.MergeParent);
        }

        return parents;
    }

    private static async Task<ITransformationComposer> ApplyTransformationsAsync(IConnection connection,
        IEnumerable<ITransformation> transformations,
        CommitEntry? commit,
        ITransformationComposer composer,
        Action<ITransformation>? beforeProcessing = null)
    {
        var tree = commit != null ? await commit.GetRootTreeAsync().ConfigureAwait(false) : null;
        var modules = await ModuleCommands.GetAsync(tree).ConfigureAwait(false);
        foreach (var transformation in transformations.OfType<ITransformationInternal>())
        {
            beforeProcessing?.Invoke(transformation);
            await transformation.Action.Invoke(tree, modules, connection.Serializer, composer).ConfigureAwait(false);
        }

        if (modules.HasAnyChange)
        {
            var stream = modules.CreateStream();
            composer.AddOrUpdate(ModuleCommands.ModuleFile, stream);
        }
        return composer;
    }

    private async Task<CommitEntry> ValidateAndUpdateBranchTipAsync(IConnection connection, string branchName, CommitEntry commit)
    {
        var tree = await commit.GetRootTreeAsync().ConfigureAwait(false);
        var parents = await commit.GetParentsAsync().ConfigureAwait(false);
        var parentTree = parents.Any() ? await parents[0].GetRootTreeAsync().ConfigureAwait(false) : null;
        if (parents.Count == 1 && tree == parentTree)
        {
            // If no change, do not create an empty commit
            return parents[0];
        }

        var validation = _treeValidation.Invoke();
        await validation.ValidateAsync(tree, connection.Model, connection.Serializer).ConfigureAwait(false);

        if (connection.Repository.Branches.TryGet(branchName, out var branch))
        {
            branch.UpdateRef(commit);
        }
        else
        {
            connection.Repository.Branches.Add(branchName, commit);
        }

        return commit;
    }

    internal static async Task<List<CommitEntry>> RetrieveParentsOfTheCommitBeingCreatedAsync(
        IGitConnection repository,
        Branch? branch,
        bool amendPreviousCommit,
        CommitEntry? mergeParent = null)
    {
        if (amendPreviousCommit)
        {
            if (branch is null)
            {
                throw new GitObjectDbNonExistingBranchException();
            }
            var tip = await branch.GetTipAsync().ConfigureAwait(false);
            return [.. await tip.GetParentsAsync().ConfigureAwait(false)];
        }

        var parents = new List<CommitEntry>();
        if (branch?.Tip is not null)
        {
            parents.Add(await branch.GetTipAsync().ConfigureAwait(false));
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
