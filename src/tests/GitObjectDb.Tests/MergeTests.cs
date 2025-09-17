using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Model;
using Models.Software;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Tests;

public class MergeTests : BranchMergerFixture
{
    /* main:      A---B    A---B
                   \    ->  \   \
       newBranch:   C        C---x */

    protected override async Task TwoDifferentPropertyEditsActAndAssertAsync(IConnection sut, Table table, string newDescription, string newName, Signature signature, CommitEntry b, CommitEntry c)
    {
        // Act
        var merge = await sut.MergeAsync("newBranch", upstreamCommittish: "main");

        // Assert
        var mergeCommit = await merge.CommitAsync(signature, signature);
        var parents = await mergeCommit.GetParentsAsync();
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        Assert.Multiple(() =>
        {
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.NonFastForward));
            Assert.That(merge.Commits, Has.Count.EqualTo(1));
            Assert.That(parents, Has.Count.EqualTo(2));
            Assert.That(parents[0], Is.EqualTo(c));
            Assert.That(parents[1], Is.EqualTo(b));
            Assert.That(newTable.Name, Is.EqualTo(newName));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }

    protected override async Task FastForwardActAndAssertAsync(IConnection sut, Table table, string newDescription, Signature signature, CommitEntry b)
    {
        // Act
        var merge = await sut.MergeAsync("newBranch", upstreamCommittish: "main");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.FastForward));
            Assert.That(merge.Commits, Has.Count.Zero);
        });
        var mergeCommit = await merge.CommitAsync(signature, signature);
        Assert.That(mergeCommit, Is.EqualTo(b));

        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        Assert.That(newTable.Description, Is.EqualTo(newDescription));
    }

    protected override async Task SamePropertyEditsActAndAssertAsync(IConnection sut, Table table, string bValue, string cValue, Signature signature)
    {
        // Act
        var merge = await sut.MergeAsync("newBranch", upstreamCommittish: "main");

        // Assert
        Assert.That(merge.Status, Is.EqualTo(MergeStatus.Conflicts));
        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<GitObjectDbException>(async () => await merge.CommitAsync(signature, signature));
            Assert.That(merge.CurrentChanges, Has.Count.EqualTo(1));
            Assert.That(merge.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.EditConflict));
            Assert.That(merge.CurrentChanges[0].Conflicts, Has.Count.EqualTo(1));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].Property.Name, Is.EqualTo(nameof(table.Description)));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].AncestorValue, Is.EqualTo(table.Description));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].OurValue, Is.EqualTo(cValue));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].TheirValue, Is.EqualTo(bValue));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].IsResolved, Is.False);
            Assert.That(merge.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.Null);
        });
        merge.CurrentChanges[0].Conflicts[0].Resolve("resolved");
        Assert.Multiple(() =>
        {
            Assert.That(merge.CurrentChanges[0].Conflicts[0].IsResolved, Is.True);
            Assert.That(merge.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.EqualTo("resolved"));
            Assert.That(merge.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.Edit));
        });

        // Act
        await merge.CommitAsync(signature, signature);

        // Assert
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        Assert.That(newTable.Description, Is.EqualTo("resolved"));
    }

    protected override async Task EditOnTheirParentDeletionActAndAssertAsync(PerformAction actionTarget, IConnection sut, Table parentTable, Field field, Signature signature)
    {
        // Act
        var merge = await sut.MergeAsync("newBranch", upstreamCommittish: "main");

        // Assert
        var conflict = merge.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        Assert.Multiple(() =>
        {
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.Conflicts));
            Assert.ThrowsAsync<GitObjectDbException>(async () => await merge.CommitAsync(signature, signature));
            Assert.That(merge.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.That(conflict.Path, Is.EqualTo(field.Path));
            Assert.That(((Node)(actionTarget == PerformAction.OnBranch ? conflict.Ours : conflict.Theirs)).Id, Is.EqualTo(field.Id));
            Assert.That(((Node)(actionTarget == PerformAction.OnBranch ? conflict.TheirRootDeletedParent : conflict.OurRootDeletedParent)).Id, Is.EqualTo(parentTable.Id));
        });

        // Act
        merge.CurrentChanges.Remove(conflict);
        await merge.CommitAsync(signature, signature);

        // Assert
        Assert.That(merge.Status, Is.EqualTo(actionTarget == PerformAction.OnBranch ? MergeStatus.NonFastForward : MergeStatus.FastForward));
    }

    protected override async Task AddOnTheirParentDeletionActAndAssertAsync(PerformAction actionTarget, IConnection sut, Signature signature)
    {
        // Act
        var merge = await sut.MergeAsync("newBranch", upstreamCommittish: "main");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.Conflicts));
            Assert.That(merge.CurrentChanges, actionTarget == PerformAction.OnMain ?
                                              Has.Count.EqualTo(1) :
                                              Has.Count.GreaterThan(1));
            Assert.That(merge.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.ThrowsAsync<GitObjectDbException>(async () => await merge.CommitAsync(signature, signature));
        });

        // Act
        var conflict = merge.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        merge.CurrentChanges.Remove(conflict);
        await merge.CommitAsync(signature, signature);

        // Assert
        Assert.That(merge.Status, Is.EqualTo(
            actionTarget == PerformAction.OnMain ? MergeStatus.FastForward : MergeStatus.NonFastForward));
    }

    protected override async Task DeleteChildNoConflictActAndAssertAsync(PerformAction actionTarget, IConnection sut, Table table, string newDescription, Field field, Signature signature)
    {
        // Act
        var merge = await sut.MergeAsync("newBranch", upstreamCommittish: "main");
        await merge.CommitAsync(signature, signature);

        // Assert
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        var missingField = await sut.LookupAsync<Field>(newBranchTip, field.Path);
        Assert.Multiple(() =>
        {
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.NonFastForward));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
            Assert.That(missingField, Is.Null);
        });
    }

    protected override async Task RenameAndEditActAndAssertAsync(IConnection sut, Field field, string newDescription, Signature signature, DataPath newPath, CommitEntry b, CommitEntry c)
    {
        // Act
        var merge = await sut.MergeAsync("newBranch", upstreamCommittish: "main");
        var mergeCommit = await merge.CommitAsync(signature, signature);

        // Assert
        var parents = await mergeCommit.GetParentsAsync();
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Field>(newBranchTip, newPath);
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await sut.LookupAsync<Field>(newBranchTip, field.Path), Is.Null);
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.NonFastForward));
            Assert.That(merge.Commits, Has.Count.EqualTo(1));
            Assert.That(parents, Has.Count.EqualTo(2));
            Assert.That(parents[0], Is.EqualTo(c));
            Assert.That(parents[1], Is.EqualTo(b));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }
}
