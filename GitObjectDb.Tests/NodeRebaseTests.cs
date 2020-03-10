using GitObjectDb.Comparison;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Models.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Linq;

namespace GitObjectDb.Tests
{
    public class NodeRebaseTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization))]
        public void TwoDifferentPropertyEdits(IConnection sut, Repository repository, Table table, string newDescription, string newName, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = repository.Head.Tip;
            var oldDescription = table.Description;
            table.Description = newDescription;
            var b = sut
                .Update(c => c.Update(table))
                .Commit("B", signature, signature);
            var branch = sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            table.Description = oldDescription;
            table.Name = newName;
            sut
                .Update(c => c.Update(table))
                .Commit("C", signature, signature);

            // Act
            var rebase = sut.Rebase(upstreamCommittish: "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.ReplayedCommits, Has.Count.EqualTo(1));
            var commitFilter = new CommitFilter
            {
                IncludeReachableFrom = repository.Head.Tip,
                SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
            };
            var commits = repository.Commits.QueryBy(commitFilter).ToList();
            Assert.That(commits[0], Is.EqualTo(a));
            Assert.That(commits[1], Is.EqualTo(b));
            Assert.That(commits[2], Is.EqualTo(rebase.CompletedCommits[0]));
            Assert.That(commits[2], Is.EqualTo(repository.Head.Tip));
            var newTable = sut.Get<Table>(table.Path);
            Assert.That(newTable.Name, Is.EqualTo(newName));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization))]
        public void SamePropertyEdits(IConnection sut, Table table, string bValue, string cValue, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var oldValue = table.Description;
            table.Description = bValue;
            sut
                .Update(c => c.Update(table))
                .Commit("B", signature, signature);
            var branch = sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            table.Description = cValue;
            sut
                .Update(c => c.Update(table))
                .Commit("C", signature, signature);

            // Act
            var rebase = sut.Rebase(upstreamCommittish: "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => rebase.Continue());
            Assert.That(rebase.CurrentChanges, Has.Count.EqualTo(1));
            Assert.That(rebase.CurrentChanges[0].Status, Is.EqualTo(NodeMergeStatus.EditConflict));
            Assert.That(rebase.CurrentChanges[0].Conflicts, Has.Count.EqualTo(1));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].Property.Name, Is.EqualTo(nameof(table.Description)));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].AncestorValue, Is.EqualTo(oldValue));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].TheirValue, Is.EqualTo(cValue));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].OurValue, Is.EqualTo(bValue));
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].IsResolved, Is.False);
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.Null);
            rebase.CurrentChanges[0].Conflicts[0].Resolve("resolved");
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].IsResolved, Is.True);
            Assert.That(rebase.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.EqualTo("resolved"));
            Assert.That(rebase.CurrentChanges[0].Status, Is.EqualTo(NodeMergeStatus.Edit));

            // Act
            Assert.That(rebase.Continue(), Is.EqualTo(RebaseStatus.Complete));

            // Assert
            var newTable = sut.Get<Table>(table.Path);
            Assert.That(newTable.Description, Is.EqualTo("resolved"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization))]
        public void EditOnTheirParentDeletion(IConnection sut, Application parentApplication, Table parentTable, Field field, string newName, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            sut
                .Update(c => c.Delete(parentTable))
                .Commit("B", signature, signature);
            var branch = sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            parentApplication.Name = newName;
            sut
                .Update(c => c.Update(field).Update(parentApplication))
                .Commit("C", signature, signature);

            // Act
            var rebase = sut.Rebase(upstreamCommittish: "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => rebase.Continue());
            Assert.That(rebase.CurrentChanges, Has.Count.EqualTo(2));
            Assert.That(rebase.CurrentChanges, Has.Exactly(1).Matches<NodeMergeChange>(c => c.Status == NodeMergeStatus.TreeConflict));
            var conflict = rebase.CurrentChanges.Single(c => c.Status == NodeMergeStatus.TreeConflict);
            Assert.That(conflict.Theirs.Id, Is.EqualTo(field.Id));
            Assert.That(conflict.OurRootDeletedParent.Id, Is.EqualTo(parentTable.Id));
            rebase.CurrentChanges.Remove(conflict);

            // Act
            Assert.That(rebase.Continue(), Is.EqualTo(RebaseStatus.Complete));

            // Assert
            var newApplication = sut.Get<Application>(parentApplication.Path);
            Assert.That(newApplication.Name, Is.EqualTo(newName));
        }
    }
}