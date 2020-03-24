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
    public class NodeMergeTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void TwoDifferentPropertyEdits(IConnection sut, Repository repository, Table table, string newDescription, string newName, Signature signature)
        {
            // master:    A---B    A---B
            //             \    ->  \   \
            // newBranch:   C        C---x

            // Arrange
            var a = repository.Head.Tip;
            var oldDescription = table.Description;
            table.Description = newDescription;
            var b = sut
                .Update(c => c.CreateOrUpdate(table))
                .Commit("B", signature, signature);
            var branch = sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            table.Description = oldDescription;
            table.Name = newName;
            var c = sut
                .Update(c => c.CreateOrUpdate(table))
                .Commit("C", signature, signature);

            // Act
            var merge = sut.Merge(upstreamCommittish: "master");

            // Assert
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.NonFastForward));
            Assert.That(merge.Commits, Has.Count.EqualTo(1));
            var mergeCommit = merge.Commit(signature, signature);
            var parents = mergeCommit.Parents.ToList();
            Assert.That(parents, Has.Count.EqualTo(2));
            Assert.That(parents[0], Is.EqualTo(c));
            Assert.That(parents[1], Is.EqualTo(b));
            var newTable = sut.Lookup<Table>(table.Path);
            Assert.That(newTable.Name, Is.EqualTo(newName));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void FastForward(IConnection sut, Repository repository, Table table, string newDescription, Signature signature)
        {
            // master:    A---B    A---B
            //             \    ->  \   \
            // newBranch:            ----x

            // Arrange
            var a = repository.Head.Tip;
            table.Description = newDescription;
            var b = sut
                .Update(c => c.CreateOrUpdate(table))
                .Commit("B", signature, signature);
            var branch = sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");

            // Act
            var merge = sut.Merge(upstreamCommittish: "master");

            // Assert
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.FastForward));
            Assert.That(merge.Commits, Has.Count.EqualTo(0));
            var mergeCommit = merge.Commit(signature, signature);
            Assert.That(mergeCommit, Is.EqualTo(b));
            var newTable = sut.Lookup<Table>(table.Path);
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void SamePropertyEdits(IConnection sut, Table table, string bValue, string cValue, Signature signature)
        {
            // master:    A---B    A---B
            //             \    ->  \   \
            // newBranch:   C        C---x

            // Arrange
            var oldValue = table.Description;
            table.Description = bValue;
            sut
                .Update(c => c.CreateOrUpdate(table))
                .Commit("B", signature, signature);
            var branch = sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            table.Description = cValue;
            sut
                .Update(c => c.CreateOrUpdate(table))
                .Commit("C", signature, signature);

            // Act
            var merge = sut.Merge(upstreamCommittish: "master");

            // Assert
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => merge.Commit(signature, signature));
            Assert.That(merge.CurrentChanges, Has.Count.EqualTo(1));
            Assert.That(merge.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.EditConflict));
            Assert.That(merge.CurrentChanges[0].Conflicts, Has.Count.EqualTo(1));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].Property.Name, Is.EqualTo(nameof(table.Description)));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].AncestorValue, Is.EqualTo(oldValue));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].OurValue, Is.EqualTo(cValue));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].TheirValue, Is.EqualTo(bValue));
            Assert.That(merge.CurrentChanges[0].Conflicts[0].IsResolved, Is.False);
            Assert.That(merge.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.Null);
            merge.CurrentChanges[0].Conflicts[0].Resolve("resolved");
            Assert.That(merge.CurrentChanges[0].Conflicts[0].IsResolved, Is.True);
            Assert.That(merge.CurrentChanges[0].Conflicts[0].ResolvedValue, Is.EqualTo("resolved"));
            Assert.That(merge.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.Edit));

            // Act
            var mergeCommit = merge.Commit(signature, signature);

            // Assert
            var newTable = sut.Lookup<Table>(table.Path);
            Assert.That(newTable.Description, Is.EqualTo("resolved"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void EditOnTheirParentDeletion(IConnection sut, Application parentApplication, Table parentTable, Field field, string newName, Signature signature)
        {
            // master:    A---B    A---B
            //             \    ->  \   \
            // newBranch:   C        C---x

            // Arrange
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            sut
                .Update(c => c.CreateOrUpdate(field).CreateOrUpdate(parentApplication))
                .Commit("C", signature, signature);
            var branch = sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            parentApplication.Name = newName;
            sut
                .Update(c => c.Delete(parentTable))
                .Commit("B", signature, signature);

            // Act
            var merge = sut.Merge(upstreamCommittish: "master");

            // Assert
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => merge.Commit(signature, signature));
            Assert.That(merge.CurrentChanges, Has.Count.EqualTo(1));
            Assert.That(merge.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            var conflict = merge.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
            Assert.That(((Node)conflict.Theirs).Id, Is.EqualTo(field.Id));
            Assert.That(((Node)conflict.OurRootDeletedParent).Id, Is.EqualTo(parentTable.Id));
            merge.CurrentChanges.Remove(conflict);

            // Act
            var mergeCommit = merge.Commit(signature, signature);

            // Assert
            Assert.That(merge.Status, Is.EqualTo(MergeStatus.FastForward));
        }
    }
}