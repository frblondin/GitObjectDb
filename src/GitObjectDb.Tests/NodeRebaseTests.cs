using AutoFixture;
using GitObjectDb.Comparison;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Models.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests
{
    public class NodeRebaseTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void EditTwoDifferentProperties(IConnection sut, Repository repository, Table table, string newDescription, string newName, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = repository.Head.Tip;
            var oldDescription = table.Description;
            table.Description = newDescription;
            var b = sut
                .Update(c => c.CreateOrUpdate(table))
                .Commit("B", signature, signature);
            sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            table.Description = oldDescription;
            table.Name = newName;
            sut
                .Update(c => c.CreateOrUpdate(table))
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
            var newTable = sut.Lookup<Table>(table.Path);
            Assert.That(newTable.Name, Is.EqualTo(newName));
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void EditSamePropertyConflict(IConnection sut, Table table, string bValue, string cValue, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

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
            var rebase = sut.Rebase(upstreamCommittish: "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => rebase.Continue());
            Assert.That(rebase.CurrentChanges, Has.Count.EqualTo(1));
            Assert.That(rebase.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.EditConflict));
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
            Assert.That(rebase.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.Edit));

            // Act
            Assert.That(rebase.Continue(), Is.EqualTo(RebaseStatus.Complete));

            // Assert
            var newTable = sut.Lookup<Table>(table.Path);
            Assert.That(newTable.Description, Is.EqualTo("resolved"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void EditOnTheirParentDeletion(IConnection sut, Application parentApplication, Table parentTable, Field field, string newName, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            sut
                .Update(c => c.Delete(parentTable))
                .Commit("B", signature, signature);
            var branch = sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            parentApplication.Name = newName;
            sut
                .Update(c => c.CreateOrUpdate(field).CreateOrUpdate(parentApplication))
                .Commit("C", signature, signature);

            // Act
            var rebase = sut.Rebase(upstreamCommittish: "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => rebase.Continue());
            Assert.That(rebase.CurrentChanges, Has.Count.EqualTo(2));
            Assert.That(rebase.CurrentChanges, Has.Exactly(1).Matches<MergeChange>(c => c.Status == ItemMergeStatus.TreeConflict));
            var conflict = rebase.CurrentChanges.Single(c => c.Status == ItemMergeStatus.TreeConflict);
            Assert.That(((Node)conflict.Theirs).Id, Is.EqualTo(field.Id));
            Assert.That(((Node)conflict.OurRootDeletedParent).Id, Is.EqualTo(parentTable.Id));
            rebase.CurrentChanges.Remove(conflict);

            // Act
            Assert.That(rebase.Continue(), Is.EqualTo(RebaseStatus.Complete));

            // Assert
            var newApplication = sut.Lookup<Application>(parentApplication.Path);
            Assert.That(newApplication.Name, Is.EqualTo(newName));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void AddChildNoConflict(IFixture fixture, IConnection sut, Repository repository, Table table, string newDescription, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = repository.Head.Tip;
            table.Description = newDescription;
            var b = sut
                .Update(c => c.CreateOrUpdate(table))
                .Commit("B", signature, signature);
            sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            var newFieldId = UniqueId.CreateNew();
            sut
                .Update(c => c.CreateOrUpdate(new Field(newFieldId)
                {
                    A = fixture.Create<NestedA[]>(),
                    SomeValue = fixture.Create<NestedA>(),
                }, parent: table))
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
            var newTable = sut.Lookup<Table>(table.Path);
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
            var newField = sut.AsQueryable(newTable).FirstOrDefault(f => f.Id == newFieldId);
            Assert.That(newField, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void AddChildConflict(IFixture fixture, IConnection sut, Repository repository, Table table, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = repository.Head.Tip;
            var b = sut
                .Update(c => c.Delete(table))
                .Commit("B", signature, signature);
            sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            var newFieldId = UniqueId.CreateNew();
            sut
                .Update(c => c.CreateOrUpdate(new Field(newFieldId)
                {
                    A = fixture.Create<NestedA[]>(),
                    SomeValue = fixture.Create<NestedA>(),
                }, parent: table))
                .Commit("C", signature, signature);

            // Act
            var rebase = sut.Rebase(upstreamCommittish: "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => rebase.Continue());
            Assert.That(rebase.CurrentChanges, Has.Count.EqualTo(1));
            Assert.That(rebase.CurrentChanges[0].Status, Is.EqualTo(ItemMergeStatus.TreeConflict));
            Assert.That(((Node)rebase.CurrentChanges[0].Theirs).Id, Is.EqualTo(newFieldId));

            // Act
            rebase.CurrentChanges.Clear();
            Assert.That(rebase.Continue(), Is.EqualTo(RebaseStatus.Complete));

            // Assert
            Assert.That(rebase.CompletedCommits, Has.Count.EqualTo(0));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void DeleteChildNoConflict(IFixture fixture, IConnection sut, Repository repository, Table table, string newDescription, Field field, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = repository.Head.Tip;
            table.Description = newDescription;
            var b = sut
                .Update(c => c.CreateOrUpdate(table))
                .Commit("B", signature, signature);
            sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            sut
                .Update(c => c.Delete(field))
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
            var newTable = sut.Lookup<Table>(table.Path);
            Assert.That(newTable.Description, Is.EqualTo(newDescription));
            var missingField = sut.AsQueryable(newTable).FirstOrDefault(f => f.Id == field.Id);
            Assert.That(missingField, Is.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void DeleteChildConflict(IFixture fixture, IConnection sut, Repository repository, Table table, Field field, string newDescription, Signature signature)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = repository.Head.Tip;
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            var b = sut
                .Update(c => c.CreateOrUpdate(field))
                .Commit("B", signature, signature);
            sut.Checkout("newBranch", createNewBranch: true, "HEAD~1");
            sut
                .Update(c => c.Delete(table))
                .Commit("C", signature, signature);

            // Act
            var rebase = sut.Rebase(upstreamCommittish: "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.Throws<GitObjectDbException>(() => rebase.Continue());
            var conflicts = rebase.CurrentChanges.Where(c => c.Status == ItemMergeStatus.TreeConflict).ToList();
            Assert.That(conflicts, Has.Count.EqualTo(1));

            // Act
            rebase.CurrentChanges.Remove(conflicts[0]);
            Assert.That(rebase.Continue(), Is.EqualTo(RebaseStatus.Complete));

            // Assert
            Assert.That(rebase.CompletedCommits, Has.Count.EqualTo(1));
        }
    }
}