using GitObjectDb.Comparison;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests;

public class CherryPickTests : BranchMergerFixture
{
    /* main:      A---B
                   \
       newBranch:   C   ->   A---C---B */

    protected override void TwoDifferentPropertyEditsActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string newDescription, string newName, Signature signature, Commit b, Commit c)
    {
        // Act
        var cherryPick = sut.CherryPick("newBranch", b.Sha, commitType: commitType);

        // Assert
        var commitFilter = new CommitFilter
        {
            IncludeReachableFrom = sut.Repository.Branches["newBranch"].Tip,
            SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
        };
        var commits = sut.Repository.Commits.QueryBy(commitFilter).ToList();
        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        Assert.Multiple(() =>
        {
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            Assert.That(commits[1], Is.EqualTo(c));
            Assert.That(commits[2], Is.EqualTo(cherryPick.CompletedCommit));
            Assert.That(commits[2], Is.EqualTo(sut.Repository.Branches["newBranch"].Tip));
            Assert.That(newTable.Name, Is.EqualTo(newName));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }

    protected override void FastForwardActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string newDescription, Signature signature, Commit b)
    {
        // Act
        var rebase = sut.CherryPick("newBranch", "main", commitType: commitType);

        // Assert
        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }

    protected override void SamePropertyEditsActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string bValue, string cValue, Signature signature)
    {
        // Act
        var cherryPick = sut.CherryPick("newBranch", "main", commitType: commitType);

        // Assert
        Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.Conflicts));
        Assert.Throws<GitObjectDbException>(() => cherryPick.CommitChanges());
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
        Assert.That(cherryPick.CommitChanges(), Is.EqualTo(CherryPickStatus.CherryPicked));

        // Assert
        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        Assert.That(newTable.Description, Is.EqualTo("resolved"));
    }

    protected override void EditOnTheirParentDeletionActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table parentTable, Field field, Signature signature)
    {
        // Act
        var cherryPick = sut.CherryPick("newBranch", "main");

        // Assert
        Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.Conflicts));
        Assert.Multiple(() =>
        {
            Assert.Throws<GitObjectDbException>(() => cherryPick.CommitChanges());
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
        Assert.That(cherryPick.CommitChanges(), Is.EqualTo(CherryPickStatus.CherryPicked));
    }

    protected override void DeleteChildNoConflictActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table table, string newDescription, Field field, Signature signature)
    {
        // Act
        var cherryPick = sut.CherryPick("newBranch", "main", commitType: commitType);

        // Assert
        Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
        var commitFilter = new CommitFilter
        {
            IncludeReachableFrom = sut.Repository.Branches["newBranch"].Tip,
            SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
        };
        var commits = sut.Repository.Commits.QueryBy(commitFilter).ToList();
        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        var missingField = sut.GetNodes<Field>("newBranch", parent: newTable).FirstOrDefault(f => f.Id == field.Id);
        Assert.Multiple(() =>
        {
            Assert.That(commits[2], Is.EqualTo(cherryPick.CompletedCommit));
            Assert.That(commits[2], Is.EqualTo(sut.Repository.Branches["newBranch"].Tip));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
            Assert.That(missingField, Is.Null);
        });
    }

    protected override void AddOnTheirParentDeletionActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Signature signature)
    {
        // Act
        var cherryPick = sut.CherryPick("newBranch", "main", commitType: commitType);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.Conflicts));
            Assert.That(cherryPick.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.Throws<GitObjectDbException>(() => cherryPick.CommitChanges());
        });

        // Act
        var conflict = cherryPick.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        cherryPick.CurrentChanges.Remove(conflict);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(cherryPick.CommitChanges(), Is.EqualTo(CherryPickStatus.CherryPicked));
            Assert.That(cherryPick.CompletedCommit, actionTarget == PerformAction.OnMain ?
                                                    Is.Null :
                                                    Is.Not.Null);
        });
    }

    protected override void RenameAndEditActAndAssert(CommitCommandType commitType, IConnection sut, Field field, string newDescription, Signature signature, DataPath newPath, Commit b, Commit c)
    {
        // Act
        var cherryPick = sut.CherryPick("newBranch", "main", commitType: commitType);

        // Assert
        var newTable = sut.Lookup<Field>("newBranch", newPath);
        Assert.Multiple(() =>
        {
            Assert.That(sut.Lookup<Field>("newBranch", field.Path), Is.Null);
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            Assert.That(cherryPick.CompletedCommit, Is.Not.Null);
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }
}
