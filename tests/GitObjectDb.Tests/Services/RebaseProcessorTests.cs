using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Rebase;
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
    public class RebaseProcessorTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseTwoDifferentPropertiesChanged(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updateName = a.With(a.Applications[0].Pages[0], p => p.Name, "modified name");
            var b = container.Commit(updateName.Repository, signature, message); // B
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var updateDescription = a.With(a.Applications[0].Pages[0], p => p.Description, "modified description");
            container.Commit(updateDescription.Repository, signature, message); // C
            var rebase = container.Rebase(sut.Id, "master");
            var repository = container.Repositories.Single();

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = repository.Execute(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(c => c.Id).ToList(), r.Head.Tip.Id);
            });
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(b.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(repository.CommitId, Is.EqualTo(tip));
            Assert.That(repository.Applications[0].Pages[0].Name, Is.EqualTo("modified name"));
            Assert.That(repository.Applications[0].Pages[0].Description, Is.EqualTo("modified description"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseChildDeletion(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updateName = a.With(a.Applications[0], app => app.Name, "modified name");
            var b = container.Commit(updateName.Repository, signature, message); // B
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var deletePage = a.With(c => c.Remove(a.Applications[0], app => app.Pages, a.Applications[0].Pages[0]));
            container.Commit(deletePage.Repository, signature, message); // C
            var rebase = container.Rebase(sut.Id, "master");
            var repository = container.Repositories.Single();

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = repository.Execute(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(c => c.Id).ToList(), r.Head.Tip.Id);
            });
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(b.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(repository.CommitId, Is.EqualTo(tip));
            Assert.That(repository.Applications[0].Name, Is.EqualTo("modified name"));
            Assert.That(repository.Applications[0].Pages, Has.Count.EqualTo(a.Applications[0].Pages.Count - 1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseChildAddition(IServiceProvider serviceProvider, ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updateName = a.With(a.Applications[0], app => app.Name, "modified name");
            var b = container.Commit(updateName.Repository, signature, message); // B
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var page = new Page(serviceProvider, UniqueId.CreateNew(), "name", "description", new LazyChildren<Field>());
            var addPage = a.With(c => c.Add(a.Applications[0], app => app.Pages, page));
            container.Commit(addPage.Repository, signature, message); // C
            var rebase = container.Rebase(sut.Id, "master");
            var repository = container.Repositories.Single();

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = repository.Execute(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(c => c.Id).ToList(), r.Head.Tip.Id);
            });
            Assert.That(commits, Has.Count.EqualTo(3));
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(b.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(repository.CommitId, Is.EqualTo(tip));
            Assert.That(repository.Applications[0].Name, Is.EqualTo("modified name"));
            Assert.That(repository.Applications[0].Pages, Has.Count.EqualTo(a.Applications[0].Pages.Count + 1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseFailsWhenUpstreamBranchContainsMigrations(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, DummyMigration migration)
        {
            // master:    A---B  (B contains a migration)
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            container.Commit(
                a.With(c => c.Add(a, r => r.Migrations, migration)).Repository,
                signature, message); // B
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            container.Commit(
                a.With(a.Applications[0].Pages[0], p => p.Description, "modified description").Repository,
                signature, message); // C
            Assert.Throws<NotSupportedException>(() => container.Rebase(sut.Id, "master"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseDetectsConflicts(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            var rebase = CreateConflictingRebase(sut, container, signature, message);

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.That(rebase.ModifiedProperties, Has.Count.EqualTo(3));
            Assert.That(rebase.ModifiedProperties, Has.Exactly(1).Matches<ObjectRepositoryPropertyChange>(c => c.IsInConflict));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseConflictsCanResolvedAndContinued(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            var rebase = CreateConflictingRebase(sut, container, signature, message);
            rebase.ModifiedProperties.Single(c => c.IsInConflict).Resolve("resolved");
            rebase.Continue();

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            Assert.That(container.Repositories.Single().Applications[0].Pages[0].Name, Is.EqualTo("resolved"));
        }

        static IObjectRepositoryRebase CreateConflictingRebase(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \    (B & C change same property)
            // newBranch:   C   ->   A---C---B
            var a = container.AddRepository(sut, signature, message); // A
            var updateName = a.With(a.Applications[0].Pages[0], p => p.Name, "foo");
            container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var updates = a.With(c => c
                .Update(a.Applications[0].Pages[0], p => p.Name, "bar")
                .Update(a.Applications[0].Pages[0], p => p.Description, "bar")
                .Update(a.Applications[0].Pages[0].Fields[0], f => f.Name, "bar"));
            container.Commit(updates, signature, message);
            return container.Rebase(sut.Id, "master");
        }
    }
}
