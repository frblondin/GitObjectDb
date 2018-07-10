using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Git.Backends;
using LibGit2Sharp;
using NUnit.Framework;
using PowerAssert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public partial class InstanceTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsPageNameUpdate(Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            var originalCommit = sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, GetRepositoryDescription(inMemoryBackend));
            var modifiedPage = page.With(p => p.Name == "modified");
            var commit = sut.Commit(modifiedPage.Instance, signature, message);

            // Act
            var changes = computeTreeChangesFactory(GetRepositoryDescription(inMemoryBackend))
                .Compare(typeof(Instance), originalCommit.Id, commit.Id);

            // Assert
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
            Assert.That(changes.Modified, Has.Count.EqualTo(1));
            Assert.That(changes.Modified[0].Old.Name, Is.EqualTo(page.Name));
            Assert.That(changes.Modified[0].New.Name, Is.EqualTo(modifiedPage.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsFieldAddition(IServiceProvider serviceProvider, Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            var originalCommit = sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, GetRepositoryDescription(inMemoryBackend));
            var field = new Field(serviceProvider, Guid.NewGuid(), "foo");
            var modifiedPage = page.With(p => p.Fields.Add(field));
            var commit = sut.Commit(modifiedPage.Instance, signature, message);

            // Act
            var changes = computeTreeChangesFactory(GetRepositoryDescription(inMemoryBackend))
                .Compare(typeof(Instance), originalCommit.Id, commit.Id);

            // Assert
            Assert.That(changes.Modified, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
            Assert.That(changes.Added, Has.Count.EqualTo(1));
            Assert.That(changes.Added[0].Old, Is.Null);
            Assert.That(changes.Added[0].New.Name, Is.EqualTo(field.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsFieldDeletion(Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            var originalCommit = sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, GetRepositoryDescription(inMemoryBackend));
            var field = page.Fields[5];
            var modifiedPage = page.With(p => p.Fields.Delete(field));
            var commit = sut.Commit(modifiedPage.Instance, signature, message);

            // Act
            var changes = computeTreeChangesFactory(GetRepositoryDescription(inMemoryBackend))
                .Compare(typeof(Instance), originalCommit.Id, commit.Id);

            // Assert
            Assert.That(changes.Modified, Is.Empty);
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Has.Count.EqualTo(1));
            Assert.That(changes.Deleted[0].New, Is.Null);
            Assert.That(changes.Deleted[0].Old.Name, Is.EqualTo(field.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsPageDeletion(Instance sut, Application application, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            var originalCommit = sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, GetRepositoryDescription(inMemoryBackend));
            var page = application.Pages[1];
            var modifiedApplication = application.With(p => p.Pages.Delete(page));
            var commit = sut.Commit(modifiedApplication.Instance, signature, message);

            // Act
            var changes = computeTreeChangesFactory(GetRepositoryDescription(inMemoryBackend))
                .Compare(typeof(Instance), originalCommit.Id, commit.Id);

            // Assert
            Assert.That(changes.Modified, Is.Empty);
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Has.Count.EqualTo(MetadataCustomization.FieldPerPageCount + 1));
            var pageDeletion = changes.Deleted.FirstOrDefault(o => o.Old is Page);
            Assert.That(pageDeletion, Is.Not.Null);
            Assert.That(pageDeletion.New, Is.Null);
            Assert.That(pageDeletion.Old.Name, Is.EqualTo(page.Name));
        }
    }
}
