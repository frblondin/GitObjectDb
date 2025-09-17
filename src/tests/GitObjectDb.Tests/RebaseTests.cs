using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Model;
using Models.Software;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Tests;

public class RebaseTests : BranchMergerFixture
{
    /* main:      A---B
                   \
       newBranch:   C   ->   A---B---C */

    protected override async Task TwoDifferentPropertyEditsActAndAssertAsync(
        IConnection sut, Table table, string newDescription, string newName,
        Signature signature, CommitEntry b, CommitEntry c)
    {
        // Act
        var rebase = await sut.RebaseAsync("newBranch", upstreamCommittish: "main");

        // Assert
        var commits = sut.Repository.GetLogAsync("newBranch", LogOptions.Default with { SortBy = LogTraversal.Topological })
            .ToEnumerable().ToList();
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.ReplayedCommits, Has.Count.EqualTo(1));
            Assert.That(commits[1].Id, Is.EqualTo(b.Id));
            Assert.That(commits[2].Id, Is.EqualTo(rebase.CompletedCommits[0].Id));
            Assert.That(commits[2].Id, Is.EqualTo(sut.Repository.Branches["newBranch"].Tip));
            Assert.That(newTable.Name, Is.EqualTo(newName));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }

    protected override async Task FastForwardActAndAssertAsync(IConnection sut, Table table, string newDescription, Signature signature, CommitEntry b)
    {
        // Act
        var rebase = await sut.RebaseAsync("newBranch", upstreamCommittish: "main");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.ReplayedCommits, Has.Count.Zero);
        });

        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        Assert.That(newTable.Description, Is.EqualTo(newDescription));
    }

    protected override async Task SamePropertyEditsActAndAssertAsync(IConnection sut, Table table, string bValue, string cValue, Signature signature)
    {
        // Act
        var rebase = await sut.RebaseAsync("newBranch", upstreamCommittish: "main");

        // Assert
        Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<GitObjectDbException>(rebase.ContinueAsync);
            Assert.That(rebase.CurrentChanges, Has.Count.EqualTo(1));
            Assert.That(rebase.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.EditConflict));
            Assert.That(rebase.CurrentChanges[0].Conflicts, Has.Count.EqualTo(1));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].Property.Name, Is.EqualTo(nameof(table.Description)));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].AncestorValue, Is.EqualTo(table.Description));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].TheirValue, Is.EqualTo(cValue));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].OurValue, Is.EqualTo(bValue));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].IsResolved, Is.False);
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.Null);
        });

        rebase.CurrentChanges[0].Conflicts[0].Resolve("resolved");
        Assert.Multiple(() =>
        {
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].IsResolved, Is.True);
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.EqualTo("resolved"));
            Assert.That(rebase.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.Edit));
        });

        // Act
        Assert.That(await rebase.ContinueAsync(), Is.EqualTo(RebaseStatus.Complete));

        // Assert
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        Assert.That(newTable.Description, Is.EqualTo("resolved"));
    }

    protected override async Task EditOnTheirParentDeletionActAndAssertAsync(
        PerformAction actionTarget, IConnection sut, Table parentTable, Field field, Signature signature)
    {
        // Act
        var rebase = await sut.RebaseAsync("newBranch", upstreamCommittish: "main");

        // Assert
        var conflict = rebase.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.ThrowsAsync<GitObjectDbException>(rebase.ContinueAsync);
            Assert.That(rebase.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.That(((Node)(actionTarget == PerformAction.OnMain ? conflict.Ours : conflict.Theirs)).Id, Is.EqualTo(field.Id));
            Assert.That(((Node)(actionTarget == PerformAction.OnMain ? conflict.TheirRootDeletedParent : conflict.OurRootDeletedParent)).Id, Is.EqualTo(parentTable.Id));
        });

        // Act
        rebase.CurrentChanges.Remove(conflict);
        Assert.That(await rebase.ContinueAsync(), Is.EqualTo(RebaseStatus.Complete));
    }

    protected override async Task AddOnTheirParentDeletionActAndAssertAsync(PerformAction actionTarget, IConnection sut, Signature signature)
    {
        // Act
        var rebase = await sut.RebaseAsync("newBranch", upstreamCommittish: "main");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.That(rebase.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.ThrowsAsync<GitObjectDbException>(rebase.ContinueAsync);
        });

        // Act
        var conflict = rebase.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        rebase.CurrentChanges.Remove(conflict);

        // Assert
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await rebase.ContinueAsync(), Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.CompletedCommits, actionTarget == PerformAction.OnMain ?
                                                 Has.Count.EqualTo(1) :
                                                 Has.Count.Zero);
        });
    }

    protected override async Task DeleteChildNoConflictActAndAssertAsync(PerformAction actionTarget, IConnection sut, Table table, string newDescription, Field field, Signature signature)
    {
        // Act
        var rebase = await sut.RebaseAsync("newBranch", upstreamCommittish: "main");

        // Assert
        var commits = sut.Repository.GetLogAsync("newBranch", LogOptions.Default with { SortBy = LogTraversal.Topological })
            .ToEnumerable().ToList();
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        var missingField = await sut.LookupAsync<Field>(newBranchTip, field.Path);
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.ReplayedCommits, Has.Count.EqualTo(1));
            Assert.That(commits[2].Id, Is.EqualTo(rebase.CompletedCommits[0].Id));
            Assert.That(commits[2].Id, Is.EqualTo(sut.Repository.Branches["newBranch"].Tip));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
            Assert.That(missingField, Is.Null);
        });
    }

    protected override async Task RenameAndEditActAndAssertAsync(IConnection sut, Field field, string newDescription, Signature signature, DataPath newPath, CommitEntry b, CommitEntry c)
    {
        // Act
        var rebase = await sut.RebaseAsync("newBranch", upstreamCommittish: "main");

        // Assert
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Field>(newBranchTip, newPath);
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await sut.LookupAsync<Field>(newBranchTip, field.Path), Is.Null);
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.CompletedCommits, Has.Count.EqualTo(1));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }
}
