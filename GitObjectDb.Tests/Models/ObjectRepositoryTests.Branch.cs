using AutoFixture;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Services;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Models.Migration;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Migrations;
using LibGit2Sharp;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PowerAssert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public partial class ObjectRepositoryTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeTwoDifferentPropertiesChanged(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            sut = container.AddRepository(sut, signature, message); // A

            // Act
            container.Branch(sut, "newBranch");
            var updateName = page.With(p => p.Name == "modified name");
            container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(sut.Id, "master");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = container.Commit(updateDescription.Repository, signature, message); // C
            var mergeCommit = container.Merge(sut.Id, "newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(container, container[sut.Id].RepositoryDescription)
                .Compare(commitC.CommitId, mergeCommit);
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Modified));
            Assert.That(changes[0].Old.Name, Is.EqualTo(page.Name));
            Assert.That(changes[0].New.Name, Is.EqualTo(updateName.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeFileAdditionChange(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, Page page, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            sut = container.AddRepository(sut, signature, message); // A

            // Act
            container.Branch(sut, "newBranch");
            var updateName = page.With(p => p.Fields.Add(new Field(serviceProvider, Guid.NewGuid(), "new field")));
            container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(sut.Id, "master");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = container.Commit(updateDescription.Repository, signature, message); // C
            var mergeCommit = container.Merge(sut.Id, "newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(container, container[sut.Id].RepositoryDescription)
                .Compare(commitC.CommitId, mergeCommit);
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Added));
            Assert.That(changes[0].New.Name, Is.EqualTo("new field"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeFileDeletionChange(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            sut = container.AddRepository(sut, signature, message); // A

            // Act
            container.Branch(sut, "newBranch");
            var updateName = page.With(p => p.Fields.Delete(page.Fields[1]));
            container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(sut.Id, "master");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = container.Commit(updateDescription.Repository, signature, message); // C
            var mergeCommit = container.Merge(sut.Id, "newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(container, container[sut.Id].RepositoryDescription)
                .Compare(commitC.CommitId, mergeCommit);
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Deleted));
            Assert.That(changes[0].Old.Id, Is.EqualTo(page.Fields[1].Id));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeTwoDifferentPropertiesWithMigrationChanged(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IFixture fixture, Page page, Signature signature, string message)
        {
            // master:    A-----D-----E---F
            //             \         /   /
            //              \   ,---'   /
            //               \ /   x   /
            // newBranch:     B---C---' (B contains a non-idempotent migration)

            // Arrange
            sut = container.AddRepository(sut, signature, message); // A

            // B, C
            container.Branch(sut, "newBranch");
            var updatedInstance = sut.With(i => i.Migrations.Add(fixture.Create<DummyMigration>()));
            var b = container.Commit(updatedInstance.Repository, signature, message); // B
            Assert.That(b.Migrations.Count, Is.GreaterThan(0));
            var updateName = b.Applications[1].Pages[1].With(p => p.Name == "modified name");
            container.Commit(updateName.Repository, signature, message); // C

            // D
            container.Checkout(sut.Id, "master");
            var updateDescription = page.With(p => p.Description == "modified description");
            container.Commit(updateDescription.Repository, signature, message); // D

            // E
            var mergeStep1 = container.Merge(sut.Id, "newBranch");
            Assert.That(mergeStep1.IsPartialMerge, Is.True);
            mergeStep1.Apply(signature); // E

            // F
            var mergeStep2 = container.Merge(sut.Id, "newBranch");
            Assert.That(mergeStep2.IsPartialMerge, Is.False);
            mergeStep2.Apply(signature); // F
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeSamePropertyDetectsConflicts(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            sut = container.AddRepository(sut, signature, message); // A

            // Act
            container.Branch(sut, "newBranch");
            var updateName = page.With(p => p.Name == "modified name");
            container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(sut.Id, "master");
            var updateNameOther = page.With(p => p.Name == "yet again modified name");
            container.Commit(updateNameOther.Repository, signature, message); // C
            Assert.Throws<RemainingConflictsException>(() => container.Merge(sut.Id, "newBranch").Apply(signature));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeSamePropertyConflict(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            sut = container.AddRepository(sut, signature, message); // A

            // Act
            container.Branch(sut, "newBranch");
            var updateName = page.With(p => p.Name == "modified name");
            container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(sut.Id, "master");
            var updateNameOther = page.With(p => p.Name == "yet again modified name");
            var commitC = container.Commit(updateNameOther.Repository, signature, message); // C
            var merge = container.Merge(sut.Id, "newBranch");
            var chunk = merge.ModifiedChunks.Single();
            chunk.Resolve(JToken.FromObject("merged name"));
            var mergeCommit = merge.Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(container, container[sut.Id].RepositoryDescription)
                .Compare(commitC.CommitId, mergeCommit);
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Modified));
            Assert.That(changes[0].Old.Name, Is.EqualTo("yet again modified name"));
            Assert.That(changes[0].New.Name, Is.EqualTo("merged name"));
        }
    }
}
