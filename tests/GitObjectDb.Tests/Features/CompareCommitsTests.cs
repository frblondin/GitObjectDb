using GitObjectDb.Models;
using GitObjectDb.Services;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Transformations;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Features
{
    public class CompareCommitsTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task ResolveDiffsPageNameUpdateAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false);
            var modifiedPage = sut.WithAsync((await (await sut.Applications)[0].Pages)[0], p => p.Name, "modified");
            var commit = await container.CommitAsync(modifiedPage.Repository, signature, message).ConfigureAwait(false);

            // Act
            var changes = (await computeTreeChangesFactory(container, sut.RepositoryDescription).CompareAsync(sut.CommitId, commit.CommitId).ConfigureAwait(false))
                .SkipIndexChanges();

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Modified));
            Assert.That(changes[0].Old.Name, Is.EqualTo((await (await sut.Applications)[0].Pages)[0].Name));
            Assert.That(changes[0].New.Name, Is.EqualTo(modifiedPage.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task ResolveDiffsFieldAdditionAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false);
            var field = new Field(serviceProvider, UniqueId.CreateNew(), "foo", FieldContent.Default);
            var modifiedPage = await sut.WithAsync(AddFieldAsync).ConfigureAwait(false);
            var commit = await container.CommitAsync(modifiedPage, signature, message).ConfigureAwait(false);

            // Act
            var changes = (await computeTreeChangesFactory(container, sut.RepositoryDescription).CompareAsync(sut.CommitId, commit.CommitId).ConfigureAwait(false))
                .SkipIndexChanges();

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Added));
            Assert.That(changes[0].Old, Is.Null);
            Assert.That(changes[0].New.Name, Is.EqualTo(field.Name));

            async Task<ITransformationComposer> AddFieldAsync(ITransformationComposer c) => c.Add((await (await sut.Applications)[0].Pages)[0], p => p.Fields, field);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task ResolveDiffsFieldDeletionAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false);
            var page = (await (await sut.Applications)[0].Pages)[0];
            var field = (await page.Fields)[5];
            var modifiedPage = sut.With(c => c.Remove(page, p => p.Fields, field));
            var commit = await container.CommitAsync(modifiedPage, signature, message).ConfigureAwait(false);

            // Act
            var changes = (await computeTreeChangesFactory(container, sut.RepositoryDescription).CompareAsync(sut.CommitId, commit.CommitId).ConfigureAwait(false))
                .SkipIndexChanges();

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Deleted));
            Assert.That(changes[0].New, Is.Null);
            Assert.That(changes[0].Old.Name, Is.EqualTo(field.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task ResolveDiffsPageDeletionAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false);
            var page = (await (await sut.Applications)[0].Pages)[1];
            var modifiedApplication = await sut.WithAsync(RemovePageAsync).ConfigureAwait(false);
            var commit = await container.CommitAsync(modifiedApplication, signature, message).ConfigureAwait(false);

            // Act
            var changes = (await computeTreeChangesFactory(container, sut.RepositoryDescription).CompareAsync(sut.CommitId, commit.CommitId).ConfigureAwait(false))
                .SkipIndexChanges();

            // Assert
            Assert.That(changes.Modified, Is.Empty);
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes, Has.Count.EqualTo(ModelCustomization.DefaultFieldPerPageCount + 1));
            var pageDeletion = changes.Deleted.FirstOrDefault(o => o.Old is Page);
            Assert.That(pageDeletion, Is.Not.Null);
            Assert.That(pageDeletion.New, Is.Null);
            Assert.That(pageDeletion.Old.Name, Is.EqualTo(page.Name));

            async Task<ITransformationComposer> RemovePageAsync(ITransformationComposer c) => c.Remove((await sut.Applications)[0], a => a.Pages, page);
        }
    }
}
