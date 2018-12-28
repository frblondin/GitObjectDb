using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Rebase;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Models.Migration;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using Newtonsoft.Json.Linq;
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
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updateName = sut.Applications[0].Pages[0].With(p => p.Name == "modified name");
            var b = container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var updateDescription = sut.Applications[0].Pages[0].With(p => p.Description == "modified description");
            container.Commit(updateDescription.Repository, signature, message); // C
            var rebase = container.Rebase(sut.Id, "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = container.Repositories.Single().Execute(r =>
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
            Assert.That(container.Repositories.Single().CommitId, Is.EqualTo(tip));
            Assert.That(container.Repositories.Single().Applications[0].Pages[0].Name, Is.EqualTo("modified name"));
            Assert.That(container.Repositories.Single().Applications[0].Pages[0].Description, Is.EqualTo("modified description"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseChildDeletion(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var application = sut.Applications[0];
            var updateName = application.With(app => app.Name == "modified name");
            var b = container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var deletePage = application.With(app => app.Pages.Delete(application.Pages[0]));
            container.Commit(deletePage.Repository, signature, message); // C
            var rebase = container.Rebase(sut.Id, "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = container.Repositories.Single().Execute(r =>
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
            Assert.That(container.Repositories.Single().CommitId, Is.EqualTo(tip));
            Assert.That(container.Repositories.Single().Applications[0].Name, Is.EqualTo("modified name"));
            Assert.That(container.Repositories.Single().Applications[0].Pages, Has.Count.EqualTo(application.Pages.Count - 1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseChildAddition(IServiceProvider serviceProvider, ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var application = sut.Applications[0];
            var updateName = application.With(app => app.Name == "modified name");
            var b = container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var page = new Page(serviceProvider, UniqueId.CreateNew(), "name", "description", new LazyChildren<Field>());
            var addPage = application.With(app => app.Pages.Add(page));
            container.Commit(addPage.Repository, signature, message); // C
            var rebase = container.Rebase(sut.Id, "master");

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = container.Repositories.Single().Execute(r =>
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
            Assert.That(container.Repositories.Single().CommitId, Is.EqualTo(tip));
            Assert.That(container.Repositories.Single().Applications[0].Name, Is.EqualTo("modified name"));
            Assert.That(container.Repositories.Single().Applications[0].Pages, Has.Count.EqualTo(application.Pages.Count + 1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseFailsWhenSourceBranchContainsMigrations(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, DummyMigration migration)
        {
            // master:    A---B  (B contains a migration)
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updatedInstance = sut.With(i => i.Migrations.Add(migration));
            container.Commit(updatedInstance.Repository, signature, message); // B
            container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var updateDescription = page.With(p => p.Description == "modified description");
            container.Commit(updateDescription.Repository, signature, message); // C
            Assert.Throws<NotSupportedException>(() => container.Rebase(sut.Id, "master"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseFailsWhenBranchContainsMigrations(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, DummyMigration migration)
        {
            // master:    A---B
            //             \
            // newBranch:   C  (C contains a migration)   ->   A---B---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updateName = page.With(p => p.Name == "modified name");
            container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var updatedInstance = sut.With(i => i.Migrations.Add(migration));
            container.Commit(updatedInstance.Repository, signature, message); // C
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
            Assert.That(rebase.ModifiedChunks, Has.Count.EqualTo(3));
            Assert.That(rebase.ModifiedChunks, Has.Exactly(1).Items.Matches<ObjectRepositoryChunkChange>(c => c.IsInConflict));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void RebaseConflictsCanResolvedAndContinued(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            var rebase = CreateConflictingRebase(sut, container, signature, message);
            rebase.ModifiedChunks.Single(c => c.IsInConflict).Resolve("resolved");
            rebase.Continue();

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            Assert.That(container.Repositories.Single().Applications[0].Pages[0].Name, Is.EqualTo("resolved"));
        }

        static IObjectRepositoryRebase CreateConflictingRebase(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \    (B & C change same value)
            // newBranch:   C   ->   A---B---C
            var a = container.AddRepository(sut, signature, message); // A
            var updateName = a.Applications[0].Pages[0].With(p => p.Name == "foo");
            container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var update = new PredicateComposer()
                .And(a.Applications[0].Pages[0], p => p.Name == "bar" && p.Description == "bar")
                .And(a.Applications[0].Pages[0].Fields[0], f => f.Name == "bar");
            container.Commit(a.With(update), signature, message);
            return container.Rebase(sut.Id, "master");
        }
    }
}
