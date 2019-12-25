using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Models.Rebase;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Models.Migration;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Transformations;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Services
{
    public class RebaseProcessorTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task RebaseTwoDifferentPropertiesChangedAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false); // A

            // Act
            var updateName = a.WithAsync((await (await a.Applications)[0].Pages)[0], p => p.Name, "modified name");
            var b = await container.CommitAsync(updateName.Repository, signature, message).ConfigureAwait(false); // B
            a = await container.CheckoutAsync(a.Id, "newBranch", createNewBranch: true, "HEAD~1").ConfigureAwait(false);
            var updateDescription = a.WithAsync((await (await a.Applications)[0].Pages)[0], p => p.Description, "modified description");
            await container.CommitAsync(updateDescription.Repository, signature, message).ConfigureAwait(false); // C
            var rebase = await container.RebaseAsync(sut.Id, "master").ConfigureAwait(false);
            var repository = container.Repositories.Single();

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = await repository.ExecuteAsync(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(c => c.Id).ToList(), r.Head.Tip.Id);
            }).ConfigureAwait(false);
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(b.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(repository.CommitId, Is.EqualTo(tip));
            var firstPage = (await (await repository.Applications)[0].Pages)[0];
            Assert.That(firstPage.Name, Is.EqualTo("modified name"));
            Assert.That(firstPage.Description, Is.EqualTo("modified description"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task RebaseChildDeletionAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false); // A

            // Act
            var updateName = a.WithAsync((await a.Applications)[0], app => app.Name, "modified name");
            var b = await container.CommitAsync(updateName.Repository, signature, message).ConfigureAwait(false); // B
            a = await container.CheckoutAsync(a.Id, "newBranch", createNewBranch: true, "HEAD~1").ConfigureAwait(false);
            var deletePage = await a.WithAsync(DeletePageTransformationAsync).ConfigureAwait(false);
            await container.CommitAsync(deletePage.Repository, signature, message).ConfigureAwait(false); // C
            var rebase = await container.RebaseAsync(sut.Id, "master").ConfigureAwait(false);
            var repository = container.Repositories.Single();

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = await repository.ExecuteAsync(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(c => c.Id).ToList(), r.Head.Tip.Id);
            }).ConfigureAwait(false);
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(b.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(repository.CommitId, Is.EqualTo(tip));
            Assert.That((await repository.Applications)[0].Name, Is.EqualTo("modified name"));
            Assert.That((await repository.Applications)[0].Pages, Has.Count.EqualTo((await (await repository.Applications)[0].Pages).Count - 1));

            async Task<ITransformationComposer> DeletePageTransformationAsync(ITransformationComposer c) =>
                c.Remove((await a.Applications)[0], app => app.Pages, (await (await a.Applications)[0].Pages)[0]);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task RebaseChildAdditionAsync(IServiceProvider serviceProvider, ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false); // A

            // Act
            var updateName = a.WithAsync((await a.Applications)[0], app => app.Name, "modified name");
            var b = await container.CommitAsync(updateName.Repository, signature, message).ConfigureAwait(false); // B
            a = await container.CheckoutAsync(a.Id, "newBranch", createNewBranch: true, "HEAD~1").ConfigureAwait(false);
            var page = new Page(serviceProvider, UniqueId.CreateNew(), "name", "description", new LazyChildren<Field>());
            var addPage = await a.WithAsync(AddPageTransformationAsync).ConfigureAwait(false);
            await container.CommitAsync(addPage.Repository, signature, message).ConfigureAwait(false); // C
            var rebase = await container.RebaseAsync(sut.Id, "master").ConfigureAwait(false);
            var repository = container.Repositories.Single();

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = await repository.ExecuteAsync(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(c => c.Id).ToList(), r.Head.Tip.Id);
            }).ConfigureAwait(false);
            Assert.That(commits, Has.Count.EqualTo(3));
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(b.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(repository.CommitId, Is.EqualTo(tip));
            Assert.That((await repository.Applications)[0].Name, Is.EqualTo("modified name"));
            Assert.That((await repository.Applications)[0].Pages, Has.Count.EqualTo((await (await repository.Applications)[0].Pages).Count + 1));

            async Task<ITransformationComposer> AddPageTransformationAsync(ITransformationComposer c) =>
                c.Add((await a.Applications)[0], app => app.Pages, page);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task RebaseFailsWhenUpstreamBranchContainsMigrationsAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, DummyMigration migration)
        {
            // master:    A---B  (B contains a migration)
            //             \
            // newBranch:   C   ->   A---C---B

            // Arrange
            var a = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false); // A

            // Act
            await container.CommitAsync(
                a.With(c => c.Add(a, r => r.Migrations, migration)).Repository,
                signature, message).ConfigureAwait(false); // B
            a = await container.CheckoutAsync(a.Id, "newBranch", createNewBranch: true, "HEAD~1").ConfigureAwait(false);
            await container.CommitAsync(
                a.WithAsync((await (await a.Applications)[0].Pages)[0], p => p.Description, "modified description").Repository,
                signature, message).ConfigureAwait(false); // C
            Assert.Throws<NotSupportedException>(() => container.RebaseAsync(sut.Id, "master"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task RebaseDetectsConflictsAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            var rebase = await CreateConflictingRebaseAsync(sut, container, signature, message).ConfigureAwait(false);

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Conflicts));
            Assert.That(rebase.ModifiedProperties, Has.Count.EqualTo(3));
            Assert.That(rebase.ModifiedProperties, Has.Exactly(1).Matches<ObjectRepositoryPropertyChange>(c => c.IsInConflict));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task RebaseConflictsCanResolvedAndContinuedAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            var rebase = await CreateConflictingRebaseAsync(sut, container, signature, message).ConfigureAwait(false);
            rebase.ModifiedProperties.Single(c => c.IsInConflict).Resolve("resolved");
            await rebase.ContinueAsync().ConfigureAwait(false);

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            Assert.That((await (await container.Repositories.Single().Applications)[0].Pages)[0].Name, Is.EqualTo("resolved"));
        }

        static async Task<IObjectRepositoryRebase> CreateConflictingRebaseAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \    (B & C change same property)
            // newBranch:   C   ->   A---C---B
            var a = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false); // A
            var updateName = a.WithAsync((await (await a.Applications)[0].Pages)[0], p => p.Name, "foo");
            await container.CommitAsync(updateName.Repository, signature, message).ConfigureAwait(false); // B
            await container.CheckoutAsync(a.Id, "newBranch", createNewBranch: true, "HEAD~1").ConfigureAwait(false);
            var updates = await a.WithAsync(MultipleChangesAsync).ConfigureAwait(false);
            await container.CommitAsync(updates, signature, message).ConfigureAwait(false);
            return await container.RebaseAsync(sut.Id, "master").ConfigureAwait(false);

            async Task<ITransformationComposer> MultipleChangesAsync(ITransformationComposer c)
            {
                var firstApplication = (await a.Applications);
                var firstPage = (await firstApplication[0].Pages)[0];
                var firstField = (await firstPage.Fields)[0];
                return c.Update(firstPage, p => p.Name, "bar")
                    .Update(firstPage, p => p.Description, "bar")
                    .Update(firstField, f => f.Name, "bar");
            };
        }
    }
}
