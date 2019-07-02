using GitObjectDb.Models;
using GitObjectDb.Services;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Features
{
    public class CompareCommitsTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ResolveDiffsPageNameUpdate(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var modifiedPage = sut.With(sut.Applications[0].Pages[0], p => p.Name, "modified");
            var commit = container.Commit(modifiedPage.Repository, signature, message);

            // Act
            var changes = computeTreeChangesFactory(container, sut.RepositoryDescription)
                .Compare(sut.CommitId, commit.CommitId)
                .SkipIndexChanges();

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Modified));
            Assert.That(changes[0].Old.Name, Is.EqualTo(sut.Applications[0].Pages[0].Name));
            Assert.That(changes[0].New.Name, Is.EqualTo(modifiedPage.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ResolveDiffsFieldAddition(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var field = new Field(serviceProvider, UniqueId.CreateNew(), "foo", FieldContent.Default);
            var modifiedPage = sut.With(c => c.Add(sut.Applications[0].Pages[0], p => p.Fields, field));
            var commit = container.Commit(modifiedPage, signature, message);

            // Act
            var changes = computeTreeChangesFactory(container, sut.RepositoryDescription)
                .Compare(sut.CommitId, commit.CommitId)
                .SkipIndexChanges();

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Added));
            Assert.That(changes[0].Old, Is.Null);
            Assert.That(changes[0].New.Name, Is.EqualTo(field.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ResolveDiffsFieldDeletion(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var page = sut.Applications[0].Pages[0];
            var field = page.Fields[5];
            var modifiedPage = sut.With(c => c.Remove(page, p => p.Fields, field));
            var commit = container.Commit(modifiedPage, signature, message);

            // Act
            var changes = computeTreeChangesFactory(container, sut.RepositoryDescription)
                .Compare(sut.CommitId, commit.CommitId)
                .SkipIndexChanges();

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Deleted));
            Assert.That(changes[0].New, Is.Null);
            Assert.That(changes[0].Old.Name, Is.EqualTo(field.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ResolveDiffsPageDeletion(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var page = sut.Applications[0].Pages[1];
            var modifiedApplication = sut.With(c => c.Remove(sut.Applications[0], a => a.Pages, page));
            var commit = container.Commit(modifiedApplication, signature, message);

            // Act
            var changes = computeTreeChangesFactory(container, sut.RepositoryDescription)
                .Compare(sut.CommitId, commit.CommitId)
                .SkipIndexChanges();

            // Assert
            Assert.That(changes.Modified, Is.Empty);
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes, Has.Count.EqualTo(ModelCustomization.DefaultFieldPerPageCount + 1));
            var pageDeletion = changes.Deleted.FirstOrDefault(o => o.Old is Page);
            Assert.That(pageDeletion, Is.Not.Null);
            Assert.That(pageDeletion.New, Is.Null);
            Assert.That(pageDeletion.Old.Name, Is.EqualTo(page.Name));
        }
    }
}
