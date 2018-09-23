using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Services;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Git.Backends;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public partial class ObjectRepositoryTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsPageNameUpdate(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message, inMemoryBackend);
            var modifiedPage = page.With(p => p.Name == "modified");
            var commit = container.Commit(modifiedPage.Repository, signature, message);

            // Act
            var changes = computeTreeChangesFactory(container, sut.RepositoryDescription)
                .Compare(sut.CommitId, commit.CommitId);

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Modified));
            Assert.That(changes[0].Old.Name, Is.EqualTo(page.Name));
            Assert.That(changes[0].New.Name, Is.EqualTo(modifiedPage.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsFieldAddition(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, Page page, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message, inMemoryBackend);
            var field = new Field(serviceProvider, Guid.NewGuid(), "foo");
            var modifiedPage = page.With(p => p.Fields.Add(field));
            var commit = container.Commit(modifiedPage.Repository, signature, message);

            // Act
            var changes = computeTreeChangesFactory(container, sut.RepositoryDescription)
                .Compare(sut.CommitId, commit.CommitId);

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Added));
            Assert.That(changes[0].Old, Is.Null);
            Assert.That(changes[0].New.Name, Is.EqualTo(field.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsFieldDeletion(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message, inMemoryBackend);
            var field = page.Fields[5];
            var modifiedPage = page.With(p => p.Fields.Delete(field));
            var commit = container.Commit(modifiedPage.Repository, signature, message);

            // Act
            var changes = computeTreeChangesFactory(container, sut.RepositoryDescription)
                .Compare(sut.CommitId, commit.CommitId);

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Deleted));
            Assert.That(changes[0].New, Is.Null);
            Assert.That(changes[0].Old.Name, Is.EqualTo(field.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsPageDeletion(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Application application, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message, inMemoryBackend);
            var page = application.Pages[1];
            var modifiedApplication = application.With(p => p.Pages.Delete(page));
            var commit = container.Commit(modifiedApplication.Repository, signature, message);

            // Act
            var changes = computeTreeChangesFactory(container, sut.RepositoryDescription)
                .Compare(sut.CommitId, commit.CommitId);

            // Assert
            Assert.That(changes.Modified, Is.Empty);
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes, Has.Count.EqualTo(MetadataCustomization.DefaultFieldPerPageCount + 1));
            var pageDeletion = changes.Deleted.FirstOrDefault(o => o.Old is Page);
            Assert.That(pageDeletion, Is.Not.Null);
            Assert.That(pageDeletion.New, Is.Null);
            Assert.That(pageDeletion.Old.Name, Is.EqualTo(page.Name));
        }
    }
}
