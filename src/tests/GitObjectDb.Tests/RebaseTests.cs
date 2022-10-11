using GitObjectDb.Comparison;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests;

public class RebaseTests : BranchMergerFixture
{
    /* main:      A---B
                   \
       newBranch:   C   ->   A---B---C */

    protected override void TwoDifferentPropertyEditsActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string newDescription, string newName, Signature signature, Commit b, Commit c)
    {
        // Act
        var rebase = sut.Rebase("newBranch", upstreamCommittish: "main", commitType: commitType);

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
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.ReplayedCommits, Has.Count.EqualTo(1));
            Assert.That(commits[1], Is.EqualTo(b));
            Assert.That(commits[2], Is.EqualTo(rebase.CompletedCommits[0]));
            Assert.That(commits[2], Is.EqualTo(sut.Repository.Branches["newBranch"].Tip));
            Assert.That(newTable.Name, Is.EqualTo(newName));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }

    protected override void FastForwardActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string newDescription, Signature signature, Commit b)
    {
        // Act
        var rebase = sut.Rebase("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.ReplayedCommits, Has.Count.Zero);
        });

        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        Assert.That(newTable.Description, Is.EqualTo(newDescription));
    }

    protected override void SamePropertyEditsActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string bValue, string cValue, Signature signature)
    {
        // Act
        var rebase = sut.Rebase("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
        Assert.Multiple(() =>
        {
            Assert.Throws<GitObjectDbException>(() => rebase.Continue());
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
        Assert.That(rebase.Continue(), Is.EqualTo(RebaseStatus.Complete));

        // Assert
        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        Assert.That(newTable.Description, Is.EqualTo("resolved"));
    }

    protected override void EditOnTheirParentDeletionActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table parentTable, Field field, Signature signature)
    {
        // Act
        var rebase = sut.Rebase("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        var conflict = rebase.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => rebase.Continue());
            Assert.That(rebase.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.That(((Node)(actionTarget == PerformAction.OnMain ? conflict.Ours : conflict.Theirs)).Id, Is.EqualTo(field.Id));
            Assert.That(((Node)(actionTarget == PerformAction.OnMain ? conflict.TheirRootDeletedParent : conflict.OurRootDeletedParent)).Id, Is.EqualTo(parentTable.Id));
        });

        // Act
        rebase.CurrentChanges.Remove(conflict);
        Assert.That(rebase.Continue(), Is.EqualTo(RebaseStatus.Complete));
    }

    protected override void AddOnTheirParentDeletionActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Signature signature)
    {
        // Act
        var rebase = sut.Rebase("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.That(rebase.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            Assert.Throws<GitObjectDbException>(() => rebase.Continue());
        });

        // Act
        var conflict = rebase.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
        rebase.CurrentChanges.Remove(conflict);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Continue(), Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.CompletedCommits, actionTarget == PerformAction.OnMain ?
                                                 Has.Count.EqualTo(1) :
                                                 Has.Count.Zero);
        });
    }

    protected override void DeleteChildNoConflictActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table table, string newDescription, Field field, Signature signature)
    {
        // Act
        var rebase = sut.Rebase("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        var commitFilter = new CommitFilter
        {
            IncludeReachableFrom = sut.Repository.Branches["newBranch"].Tip,
            SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
        };
        var commits = sut.Repository.Commits.QueryBy(commitFilter).ToList();
        var newTable = sut.Lookup<Table>("newBranch", table.Path);
        var missingField = sut.Lookup<Field>("newBranch", field.Path);
        Assert.Multiple(() =>
        {
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.ReplayedCommits, Has.Count.EqualTo(1));
            Assert.That(commits[2], Is.EqualTo(rebase.CompletedCommits[0]));
            Assert.That(commits[2], Is.EqualTo(sut.Repository.Branches["newBranch"].Tip));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
            Assert.That(missingField, Is.Null);
        });
    }

    protected override void RenameAndEditActAndAssert(CommitCommandType commitType, IConnection sut, Field field, string newDescription, Signature signature, DataPath newPath, Commit b, Commit c)
    {
        // Act
        var rebase = sut.Rebase("newBranch", upstreamCommittish: "main", commitType: commitType);

        // Assert
        var newTable = sut.Lookup<Field>("newBranch", newPath);
        Assert.Multiple(() =>
        {
            Assert.That(sut.Lookup<Field>("newBranch", field.Path), Is.Null);
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.CompletedCommits, Has.Count.EqualTo(1));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        });
    }
}
