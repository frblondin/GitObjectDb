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

namespace GitObjectDb.Tests.Services
{
    public class MergeProcessorTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void MergeTwoDifferentPropertiesChanged(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            sut = container.AddRepository(sut, signature, message); // A

            // Act
            sut = container.Checkout(sut.Id, "newBranch", createNewBranch: true);
            var updateName = sut.With(sut.Applications[0].Pages[0], p => p.Name, "modified name");
            container.Commit(updateName.Repository, signature, message); // B
            var a = container.Checkout(sut.Id, "master");
            var updateDescription = a.With(a.Applications[0].Pages[0], p => p.Description, "modified description");
            var commitC = container.Commit(updateDescription.Repository, signature, message); // C
            var mergeCommit = container.Merge(sut.Id, "newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(container, container[sut.Id].RepositoryDescription)
                .Compare(commitC.CommitId, mergeCommit)
                .SkipIndexChanges();
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Modified));
            Assert.That(changes[0].Old.Name, Is.EqualTo(sut.Applications[0].Pages[0].Name));
            Assert.That(changes[0].New.Name, Is.EqualTo(updateName.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void MergeFileAdditionChange(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            sut = container.AddRepository(sut, signature, message); // A

            // Act
            sut = container.Checkout(sut.Id, "newBranch", createNewBranch: true);
            var updateName = sut.With(c => c.Add(sut.Applications[0].Pages[0], p => p.Fields, new Field(serviceProvider, UniqueId.CreateNew(), "new field", FieldContent.Default)));
            container.Commit(updateName.Repository, signature, message); // B
            var a = container.Checkout(sut.Id, "master");
            var updateDescription = a.With(a.Applications[0].Pages[0], p => p.Description, "modified description");
            var commitC = container.Commit(updateDescription.Repository, signature, message); // C
            var mergeCommit = container.Merge(sut.Id, "newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(container, container[sut.Id].RepositoryDescription)
                .Compare(commitC.CommitId, mergeCommit)
                .SkipIndexChanges();
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Added));
            Assert.That(changes[0].New.Name, Is.EqualTo("new field"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void MergeFileDeletionChange(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            a = container.Checkout(a.Id, "newBranch", createNewBranch: true);
            var page = a.Applications[0].Pages[0];
            var updateName = a.With(c => c.Remove(page, p => p.Fields, page.Fields[1]));
            container.Commit(updateName, signature, message); // B
            a = container.Checkout(a.Id, "master");
            var updateDescription = a.With(a.Applications[0].Pages[0], p => p.Description, "modified description");
            var commitC = container.Commit(updateDescription.Repository, signature, message); // C
            var mergeCommit = container.Merge(sut.Id, "newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(container, container[sut.Id].RepositoryDescription)
                .Compare(commitC.CommitId, mergeCommit)
                .SkipIndexChanges();
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Deleted));
            Assert.That(changes[0].Old.Id, Is.EqualTo(page.Fields[1].Id));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void MergeTwoDifferentPropertiesWithMigrationChanged(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IFixture fixture, Signature signature, string message)
        {
            // master:    A-----D-----E---F
            //             \         /   /
            //              \   ,---'   /
            //               \ /   x   /
            // newBranch:     B---C---' (B contains a non-idempotent migration)

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // B, C
            a = container.Checkout(sut.Id, "newBranch", createNewBranch: true);
            var updatedInstance = a.With(c => c.Add(a, r => r.Migrations, fixture.Create<DummyMigration>()));
            var b = container.Commit(updatedInstance, signature, message); // B
            Assert.That(b.Migrations.Count, Is.GreaterThan(0));
            var updateName = b.With(b.Applications[1].Pages[1], p => p.Name, "modified name");
            container.Commit(updateName.Repository, signature, message); // C

            // D
            a = container.Checkout(sut.Id, "master");
            var updateDescription = a.With(a.Applications[0].Pages[0], p => p.Description, "modified description");
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
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void MergeSamePropertyDetectsConflicts(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            a = container.Checkout(sut.Id, "newBranch", createNewBranch: true);
            var updateName = a.With(a.Applications[0].Pages[0], p => p.Name, "modified name");
            container.Commit(updateName.Repository, signature, message); // B
            a = container.Checkout(sut.Id, "master");
            var updateNameOther = a.With(a.Applications[0].Pages[0], p => p.Name, "yet again modified name");
            container.Commit(updateNameOther.Repository, signature, message); // C
            Assert.Throws<RemainingConflictsException>(() => container.Merge(sut.Id, "newBranch").Apply(signature));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void MergeSamePropertyConflict(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            a = container.Checkout(sut.Id, "newBranch", createNewBranch: true);
            var updateName = a.With(a.Applications[0].Pages[0], p => p.Name, "modified name");
            container.Commit(updateName.Repository, signature, message); // B
            a = container.Checkout(sut.Id, "master");
            var updateNameOther = a.With(a.Applications[0].Pages[0], p => p.Name, "yet again modified name");
            var commitC = container.Commit(updateNameOther.Repository, signature, message); // C
            var merge = container.Merge(sut.Id, "newBranch");
            var chunk = merge.ModifiedChunks.Single();
            chunk.Resolve("merged name");
            var mergeCommit = merge.Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(container, container[sut.Id].RepositoryDescription)
                .Compare(commitC.CommitId, mergeCommit)
                .SkipIndexChanges();
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Modified));
            Assert.That(changes[0].Old.Name, Is.EqualTo("yet again modified name"));
            Assert.That(changes[0].New.Name, Is.EqualTo("merged name"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PullNotRequiringAnyMerge(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            var clientContainer = containerFactory.Create<ObjectRepository>(tempPath);
            clientContainer.Clone(container.Repositories.Single().RepositoryDescription.Path);

            // Arrange - Update source repository
            var change = sut.With(sut.Applications[0].Pages[0], p => p.Description, "foo");
            var commitResult = container.Commit(change.Repository, signature, message);

            // Act
            var pullResult = clientContainer.Pull(clientContainer.Repositories.Single().Id);
            pullResult.Apply(signature);

            // Assert
            ObjectRepositoryContainer.EnsureHeadCommit(clientContainer.Repositories.Single());
            Assert.That(pullResult.RequiresMergeCommit, Is.False);
            Assert.That(clientContainer.Repositories.Single().CommitId, Is.EqualTo(commitResult.CommitId));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PullRequiringMerge(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            var clientContainer = containerFactory.Create<ObjectRepository>(tempPath);
            clientContainer.Clone(container.Repositories.Single().RepositoryDescription.Path);

            // Arrange - Update source repository
            var change = sut.With(sut.Applications[0].Pages[0], p => p.Description, "foo");
            sut = container.Commit(change.Repository, signature, message);

            // Arrange - Update client repository
            var clientChange = sut.With(sut.Applications[0].Pages[0], p => p.Name, "bar");
            clientContainer.Commit(clientChange.Repository, signature, message);

            // Act
            var pullResult = clientContainer.Pull(clientContainer.Repositories.Single().Id);
            pullResult.Apply(signature);

            // Assert
            Assert.That(pullResult.RequiresMergeCommit, Is.True);
            Assert.That(clientContainer.Repositories.Single().Applications[0].Pages[0].Description, Is.EqualTo("foo"));
        }
    }
}
