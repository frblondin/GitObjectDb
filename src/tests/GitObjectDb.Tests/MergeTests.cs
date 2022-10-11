using GitObjectDb.Comparison;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests;

public class MergeTests : BranchMergerFixture
{
    /* main:      A---B    A---B
                   \    ->  \   \
       newBranch:   C        C---x */

    protected override void TwoDifferentPropertyEditsActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string newDescription, string newName, Signature signature, Commit b, Commit c)
    {
        // Act
        var merge = sut.Merge("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        var mergeCommit = merge.Commit(signature, signature);
        var parents = mergeCommit.Parents.ToList();
        var newTable = sut.Lookup<Table>("newBranch", table.Path);
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

    protected override void FastForwardActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string newDescription, Signature signature, Commit b)
    {
        // Act
        var merge = sut.Merge("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.FastForward));
            Assert.That(merge.Commits, Has.Count.Zero);
        });
        var mergeCommit = merge.Commit(signature, signature);
        Assert.That(mergeCommit, Is.EqualTo(b));

        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        Assert.That(newTable.Description, Is.EqualTo(newDescription));
    }

    protected override void SamePropertyEditsActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string bValue, string cValue, Signature signature)
    {
        // Act
        var merge = sut.Merge("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        Assert.That(merge.Status, Is.EqualTo(MergeStatus.Conflicts));
        Assert.Multiple(() =>
        {
            Assert.Throws<GitObjectDbException>(() => merge.Commit(signature, signature));
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
        merge.Commit(signature, signature);

        // Assert
        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        Assert.That(newTable.Description, Is.EqualTo("resolved"));
    }

    protected override void EditOnTheirParentDeletionActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table parentTable, Field field, Signature signature)
    {
        // Act
        var merge = sut.Merge("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        var conflict = merge.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        Assert.Multiple(() =>
        {
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => merge.Commit(signature, signature));
            Assert.That(merge.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.That(conflict.Path, Is.EqualTo(field.Path));
            Assert.That(((Node)(actionTarget == PerformAction.OnBranch ? conflict.Ours : conflict.Theirs)).Id, Is.EqualTo(field.Id));
            Assert.That(((Node)(actionTarget == PerformAction.OnBranch ? conflict.TheirRootDeletedParent : conflict.OurRootDeletedParent)).Id, Is.EqualTo(parentTable.Id));
        });

        // Act
        merge.CurrentChanges.Remove(conflict);
        merge.Commit(signature, signature);

        // Assert
        Assert.That(merge.Status, Is.EqualTo(actionTarget == PerformAction.OnBranch ? MergeStatus.NonFastForward : MergeStatus.FastForward));
    }

    protected override void AddOnTheirParentDeletionActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Signature signature)
    {
        // Act
        var merge = sut.Merge("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.Conflicts));
            Assert.That(merge.CurrentChanges, actionTarget == PerformAction.OnMain ?
                                              Has.Count.EqualTo(1) :
                                              Has.Count.GreaterThan(1));
            Assert.That(merge.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.Throws<GitObjectDbException>(() => merge.Commit(signature, signature));
        });

        // Act
        var conflict = merge.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        merge.CurrentChanges.Remove(conflict);
        merge.Commit(signature, signature);

        // Assert
        Assert.That(merge.Status, Is.EqualTo(
            actionTarget == PerformAction.OnMain ? MergeStatus.FastForward : MergeStatus.NonFastForward));
    }

    protected override void DeleteChildNoConflictActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table table, string newDescription, Field field, Signature signature)
    {
        // Act
        var merge = sut.Merge("newBranch", upstreamCommittish: "main", commitType: commitType);
        merge.Commit(signature, signature);

        // Assert
        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        var missingField = sut.Lookup<Field>("newBranch", field.Path);
        Assert.Multiple(() =>
        {
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.NonFastForward));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
            Assert.That(missingField, Is.Null);
        });
    }

    protected override void RenameAndEditActAndAssert(CommitCommandType commitType, IConnection sut, Field field, string newDescription, Signature signature, DataPath newPath, Commit b, Commit c)
    {
        // Act
        var merge = sut.Merge("newBranch", upstreamCommittish: "main", commitType: commitType);
        var mergeCommit = merge.Commit(signature, signature);

        // Assert
        var parents = mergeCommit.Parents.ToList();
        var newTable = sut.Lookup<Field>("newBranch", newPath);
        Assert.Multiple(() =>
        {
            Assert.That(sut.Lookup<Field>("newBranch", field.Path), Is.Null);
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.NonFastForward));
            Assert.That(merge.Commits, Has.Count.EqualTo(1));
            Assert.That(parents, Has.Count.EqualTo(2));
            Assert.That(parents[0], Is.EqualTo(c));
            Assert.That(parents[1], Is.EqualTo(b));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }
}
