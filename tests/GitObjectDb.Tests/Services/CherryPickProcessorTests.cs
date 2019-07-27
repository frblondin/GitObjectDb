using GitObjectDb.Models;
using GitObjectDb.Models.CherryPick;
using GitObjectDb.Models.Compare;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Models.Migration;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Services
{
    public class CherryPickProcessorTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CherryPickTwoDifferentPropertiesChanged(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B---C
            //             \
            // newBranch:   D   ->   A---D---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var b = container.Commit(
                a.With(a.Applications[0].Pages[0], p => p.Description, "unpicked modified description").Repository,
                signature, message); // B
            var c = container.Commit(
                b.With(b.Applications[0].Pages[0], p => p.Name, "modified name").Repository,
                signature, message); // C
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~2");
            var d = container.Commit(
                a.With(a.Applications[0].Pages[0], p => p.Description, "modified description").Repository,
                signature, message); // D
            var cherryPick = container.CherryPick(sut.Id, c.CommitId);
            var repository = container.Repositories.Single();

            // Assert
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            var (commits, tip) = repository.Execute(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(commit => commit.Id).ToList(), r.Head.Tip.Id);
            });
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(d.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(repository.CommitId, Is.EqualTo(tip));
            Assert.That(repository.Applications[0].Pages[0].Name, Is.EqualTo("modified name"));
            Assert.That(repository.Applications[0].Pages[0].Description, Is.EqualTo("modified description"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CherryPickChildDeletion(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B---C
            //             \
            // newBranch:   D   ->   A---D---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var b = container.Commit(
                a.With(a.Applications[0], app => app.Name, "unpicked modified name").Repository,
                signature, message); // B
            var c = container.Commit(
                b.With(comp => comp.Remove(b.Applications[0], app => app.Pages, b.Applications[0].Pages[0])).Repository,
                signature, message); // C
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~2");
            var d = container.Commit(
                a.With(a.Applications[0], app => app.Name, "modified name").Repository,
                signature, message); // D
            var cherryPick = container.CherryPick(sut.Id, c.CommitId);
            var repository = container.Repositories.Single();

            // Assert
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            var (commits, tip) = repository.Execute(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(commit => commit.Id).ToList(), r.Head.Tip.Id);
            });
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(d.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(repository.CommitId, Is.EqualTo(tip));
            Assert.That(repository.Applications[0].Name, Is.EqualTo("modified name"));
            Assert.That(repository.Applications[0].Pages, Has.Count.EqualTo(a.Applications[0].Pages.Count - 1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CherryPickChildAddition(IServiceProvider serviceProvider, ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B---C
            //             \
            // newBranch:   D   ->   A---D---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var b = container.Commit(
                a.With(a.Applications[0], app => app.Name, "unpicked modified name").Repository,
                signature, message); // B
            var page = new Page(serviceProvider, UniqueId.CreateNew(), "name", "description", new LazyChildren<Field>());
            var c = container.Commit(
                b.With(comp => comp.Add(b.Applications[0], app => app.Pages, page)).Repository,
                signature, message); // C
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~2");
            var d = container.Commit(
                a.With(a.Applications[0], app => app.Name, "modified name").Repository,
                signature, message); // D
            var cherryPick = container.CherryPick(sut.Id, c.CommitId);
            var repository = container.Repositories.Single();

            // Assert
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            var (commits, tip) = repository.Execute(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(commit => commit.Id).ToList(), r.Head.Tip.Id);
            });
            Assert.That(commits, Has.Count.EqualTo(3));
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(d.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(repository.CommitId, Is.EqualTo(tip));
            Assert.That(repository.Applications[0].Name, Is.EqualTo("modified name"));
            Assert.That(repository.Applications[0].Pages, Has.Count.EqualTo(a.Applications[0].Pages.Count + 1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CherryPickFailsWhenUpstreamBranchContainsMigrations(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, DummyMigration migration)
        {
            // master:    A---B  (B contains a migration)
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var b = container.Commit(
                a.With(c => c.Add(a, r => r.Migrations, migration)).Repository,
                signature, message); // B
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            container.Commit(
                a.With(a.Applications[0].Pages[0], p => p.Description, "modified description").Repository,
                signature, message); // C
            Assert.Throws<NotSupportedException>(() => container.CherryPick(sut.Id, b.CommitId));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CherryPickDetectsConflicts(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            var cherryPick = CreateConflictingCherryPick(sut, container, signature, message);

            // Assert
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.Conflicts));
            Assert.That(cherryPick.ModifiedProperties, Has.Count.EqualTo(1));
            Assert.That(cherryPick.ModifiedProperties, Has.Exactly(1).Matches<ObjectRepositoryPropertyChange>(c => c.IsInConflict));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CherryPickConflictsCanResolvedAndContinued(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            var cherryPick = CreateConflictingCherryPick(sut, container, signature, message);
            cherryPick.ModifiedProperties.Single(c => c.IsInConflict).Resolve("resolved");
            cherryPick.Commit();

            // Assert
            Assert.That(cherryPick.Status, Is.EqualTo(CherryPickStatus.CherryPicked));
            Assert.That(container.Repositories.Single().Applications[0].Pages[0].Name, Is.EqualTo("resolved"));
        }

        static IObjectRepositoryCherryPick CreateConflictingCherryPick(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \    (B & C change same property)
            // newBranch:   C   ->   A---C---B
            var a = container.AddRepository(sut, signature, message); // A
            var b = container.Commit(
                a.With(a.Applications[0].Pages[0], p => p.Name, "foo").Repository, signature, message); // B
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            container.Commit(
                a.With(c => c
                 .Update(a.Applications[0].Pages[0], p => p.Name, "bar")
                 .Update(a.Applications[0].Pages[0], p => p.Description, "bar")
                 .Update(a.Applications[0].Pages[0].Fields[0], f => f.Name, "bar")),
                signature, message);
            return container.CherryPick(sut.Id, b.CommitId);
        }
    }
}
