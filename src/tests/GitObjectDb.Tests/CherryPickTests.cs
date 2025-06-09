using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Model;
using Models.Software;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Tests;

public class CherryPickTests : BranchMergerFixture
{
    /* main:      A---B
                   \
       newBranch:   C   ->   A---C---B */

    protected override async Task TwoDifferentPropertyEditsActAndAssertAsync(IConnection sut, Table table, string newDescription, string newName, Signature signature, CommitEntry b, CommitEntry c)
    {
        // Act
        var cherryPick = await sut.CherryPickAsync("newBranch", b.Id.ToString());

        // Assert
        var commits = sut.Repository.GetLogAsync("newBranch", LogOptions.Default with { SortBy = LogTraversal.Topological })
            .ToEnumerable().ToList();
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        Assert.Multiple(() =>
        {
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            Assert.That(commits[1].Id, Is.EqualTo(c.Id));
            Assert.That(commits[2].Id, Is.EqualTo(cherryPick.CompletedCommit.Id));
            Assert.That(commits[2].Id, Is.EqualTo(sut.Repository.Branches["newBranch"].Tip));
            Assert.That(newTable.Name, Is.EqualTo(newName));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }

    protected override async Task FastForwardActAndAssertAsync(IConnection sut, Table table, string newDescription, Signature signature, CommitEntry b)
    {
        // Act
        var rebase = await sut.CherryPickAsync("newBranch", "main");

        // Assert
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }

    protected override async Task SamePropertyEditsActAndAssertAsync(IConnection sut, Table table, string bValue, string cValue, Signature signature)
    {
        // Act
        var cherryPick = await sut.CherryPickAsync("newBranch", "main");

        // Assert
        Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.Conflicts));
        Assert.ThrowsAsync<GitObjectDbException>(async () => await cherryPick.CommitChangesAsync());
        Assert.Multiple(() =>
        {
            Assert.That(cherryPick.CurrentChanges, Has.Count.EqualTo(1));
            Assert.That(cherryPick.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.EditConflict));
            Assert.That(cherryPick.CurrentChanges[0].Conflicts, Has.Count.EqualTo(1));
            Assert.That(cherryPick.CurrentChanges[0].Conflicts[0].Property.Name, Is.EqualTo(nameof(table.Description)));
            Assert.That(cherryPick.CurrentChanges[0].Conflicts[0].AncestorValue, Is.EqualTo(table.Description));
            Assert.That(cherryPick.CurrentChanges[0].Conflicts[0].TheirValue, Is.EqualTo(bValue));
            Assert.That(cherryPick.CurrentChanges[0].Conflicts[0].OurValue, Is.EqualTo(cValue));
            Assert.That(cherryPick.CurrentChanges[0].Conflicts[0].IsResolved, Is.False);
            Assert.That(cherryPick.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.Null);
        });
        cherryPick.CurrentChanges[0].Conflicts[0].Resolve("resolved");
        Assert.Multiple(() =>
        {
            Assert.That(cherryPick.CurrentChanges[0].Conflicts[0].IsResolved, Is.True);
            Assert.That(cherryPick.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.EqualTo("resolved"));
            Assert.That(cherryPick.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.Edit));
        });

        // Act
        Assert.That(await cherryPick.CommitChangesAsync(), Is.EqualTo(CherryPickStatus.CherryPicked));

        // Assert
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        Assert.That(newTable.Description, Is.EqualTo("resolved"));
    }

    protected override async Task EditOnTheirParentDeletionActAndAssertAsync(PerformAction actionTarget, IConnection sut, Table parentTable, Field field, Signature signature)
    {
        // Act
        var cherryPick = await sut.CherryPickAsync("newBranch", "main");

        // Assert
        Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.Conflicts));
        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<GitObjectDbException>(async () => await cherryPick.CommitChangesAsync());
            Assert.That(cherryPick.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
        });
        var conflict = cherryPick.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        Assert.Multiple(() =>
        {
            Assert.That(((Node)(actionTarget == PerformAction.OnMain ? conflict.Theirs : conflict.Ours)).Id, Is.EqualTo(field.Id));
            Assert.That(((Node)(actionTarget == PerformAction.OnMain ? conflict.OurRootDeletedParent : conflict.TheirRootDeletedParent)).Id, Is.EqualTo(parentTable.Id));
        });
        cherryPick.CurrentChanges.Remove(conflict);

        // Act
        Assert.That(await cherryPick.CommitChangesAsync(), Is.EqualTo(CherryPickStatus.CherryPicked));
    }

    protected override async Task DeleteChildNoConflictActAndAssertAsync(PerformAction actionTarget, IConnection sut, Table table, string newDescription, Field field, Signature signature)
    {
        // Act
        var cherryPick = await sut.CherryPickAsync("newBranch", "main");

        // Assert
        Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
        var commits = sut.Repository.GetLogAsync("newBranch", LogOptions.Default with { SortBy = LogTraversal.Topological })
            .ToEnumerable().ToList();
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Table>(newBranchTip, table.Path);
        var missingField = sut.GetNodesAsync<Field>(newBranchTip, parent: newTable).ToEnumerable().FirstOrDefault(f => f.Id == field.Id);
        Assert.Multiple(() =>
        {
            Assert.That(commits[2].Id, Is.EqualTo(cherryPick.CompletedCommit.Id));
            Assert.That(commits[2].Id, Is.EqualTo(sut.Repository.Branches["newBranch"].Tip));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
            Assert.That(missingField, Is.Null);
        });
    }

    protected override async Task AddOnTheirParentDeletionActAndAssertAsync(PerformAction actionTarget, IConnection sut, Signature signature)
    {
        // Act
        var cherryPick = await sut.CherryPickAsync("newBranch", "main");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.Conflicts));
            Assert.That(cherryPick.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.ThrowsAsync<GitObjectDbException>(async () => await cherryPick.CommitChangesAsync());
        });

        // Act
        var conflict = cherryPick.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        cherryPick.CurrentChanges.Remove(conflict);

        // Assert
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await cherryPick.CommitChangesAsync(), Is.EqualTo(CherryPickStatus.CherryPicked));
            Assert.That(cherryPick.CompletedCommit, actionTarget == PerformAction.OnMain ?
                                                    Is.Null :
                                                    Is.Not.Null);
        });
    }

    protected override async Task RenameAndEditActAndAssertAsync(IConnection sut, Field field, string newDescription, Signature signature, DataPath newPath, CommitEntry b, CommitEntry c)
    {
        // Act
        var cherryPick = await sut.CherryPickAsync("newBranch", "main");

        // Assert
        var newBranchTip = await sut.Repository.GetCommittishAsync("newBranch");
        var newTable = await sut.LookupAsync<Field>(newBranchTip, newPath);
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(await sut.LookupAsync<Field>(newBranchTip, field.Path), Is.Null);
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            Assert.That(cherryPick.CompletedCommit, Is.Not.Null);
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }
}
