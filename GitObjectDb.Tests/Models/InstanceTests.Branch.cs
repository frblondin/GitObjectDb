using AutoFixture;
using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Git.Backends;
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
    public partial class InstanceTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeTwoDifferentPropertiesChanged(IInstanceLoader loader, Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            var repositoryDescription = GetRepositoryDescription();
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, repositoryDescription); // A

            // Act
            sut.Branch("newBranch");
            var updateName = page.With(p => p.Name == "modified name");
            sut.Commit(updateName.Instance, signature, message); // B
            sut.Checkout("master");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = sut.Commit(updateDescription.Instance, signature, message); // C
            var loaded = loader.LoadFrom<Instance>(GetRepositoryDescription());
            var mergeCommit = loaded.Merge("newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(GetRepositoryDescription())
                .Compare(typeof(Instance), commitC.Id, mergeCommit);
            Assert.That(changes.Modified, Has.Count.EqualTo(1));
            Assert.That(changes.Modified[0].Old.Name, Is.EqualTo(page.Name));
            Assert.That(changes.Modified[0].New.Name, Is.EqualTo(updateName.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeFileAdditionChange(IServiceProvider serviceProvider, IInstanceLoader loader, Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            var repositoryDescription = GetRepositoryDescription();
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, repositoryDescription); // A

            // Act
            sut.Branch("newBranch");
            var updateName = page.With(p => p.Fields.Add(new Field(serviceProvider, Guid.NewGuid(), "new field")));
            sut.Commit(updateName.Instance, signature, message); // B
            sut.Checkout("master");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = sut.Commit(updateDescription.Instance, signature, message); // C
            var loaded = loader.LoadFrom<Instance>(GetRepositoryDescription());
            var mergeCommit = loaded.Merge("newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(GetRepositoryDescription())
                .Compare(typeof(Instance), commitC.Id, mergeCommit);
            Assert.That(changes.Added, Has.Count.EqualTo(1));
            Assert.That(changes.Added[0].New.Name, Is.EqualTo("new field"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeFileDeletionChange(IInstanceLoader loader, Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            var repositoryDescription = GetRepositoryDescription();
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, repositoryDescription); // A

            // Act
            sut.Branch("newBranch");
            var updateName = page.With(p => p.Fields.Delete(page.Fields[1]));
            sut.Commit(updateName.Instance, signature, message); // B
            sut.Checkout("master");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = sut.Commit(updateDescription.Instance, signature, message); // C
            var loaded = loader.LoadFrom<Instance>(GetRepositoryDescription());
            var mergeCommit = loaded.Merge("newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(GetRepositoryDescription())
                .Compare(typeof(Instance), commitC.Id, mergeCommit);
            Assert.That(changes.Deleted, Has.Count.EqualTo(1));
            Assert.That(changes.Deleted[0].Old.Id, Is.EqualTo(page.Fields[1].Id));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeTwoDifferentPropertiesWithMigrationChanged(IFixture fixture, IInstanceLoader loader, Instance sut, Page page, Signature signature, string message)
        {
            // master:    A-----D-----E---F
            //             \         /   /
            //              \   ,---'   /
            //               \ /   x   /
            // newBranch:     B---C---' (B contains a non-idempotent migration)

            // Arrange
            var repositoryDescription = GetRepositoryDescription();
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, repositoryDescription); // A

            // B, C
            sut.Branch("newBranch");
            var updatedInstance = sut.With(i => i.Migrations.Add(fixture.Create<Migration>()));
            var commitB = sut.Commit(updatedInstance.Instance, signature, message); // B
            var loaded = loader.LoadFrom<Instance>(GetRepositoryDescription());
            var updateName = loaded.Applications[1].Pages[1].With(p => p.Name == "modified name");
            loaded.Commit(updateName.Instance, signature, message); // C

            // D
            sut.Checkout("master");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = sut.Commit(updateDescription.Instance, signature, message); // D
            loaded = loader.LoadFrom<Instance>(GetRepositoryDescription());

            // E
            var mergeStep1 = loaded.Merge("newBranch");
            Assert.That(mergeStep1.IsPartialMerge, Is.True);
            var mergeCommit1 = mergeStep1.Apply(signature); // E

            // F
            loaded = loader.LoadFrom<Instance>(GetRepositoryDescription());
            var mergeStep2 = loaded.Merge("newBranch");
            Assert.That(mergeStep2.IsPartialMerge, Is.False);
            var mergeCommit2 = mergeStep2.Apply(signature); // F
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeSamePropertyDetectsConflicts(IInstanceLoader loader, Instance sut, Page page, Signature signature, string message)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            var repositoryDescription = GetRepositoryDescription();
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, repositoryDescription); // A

            // Act
            sut.Branch("newBranch");
            var updateName = page.With(p => p.Name == "modified name");
            sut.Commit(updateName.Instance, signature, message); // B
            sut.Checkout("master");
            var updateNameOther = page.With(p => p.Name == "yet again modified name");
            sut.Commit(updateNameOther.Instance, signature, message); // C
            var loaded = loader.LoadFrom<Instance>(GetRepositoryDescription());
            Assert.Throws<RemainingConflictsException>(() => loaded.Merge("newBranch").Apply(signature));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeSamePropertyConflict(IInstanceLoader loader, Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B---'

            // Arrange
            var repositoryDescription = GetRepositoryDescription();
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, repositoryDescription); // A

            // Act
            sut.Branch("newBranch");
            var updateName = page.With(p => p.Name == "modified name");
            sut.Commit(updateName.Instance, signature, message); // B
            sut.Checkout("master");
            var updateNameOther = page.With(p => p.Name == "yet again modified name");
            var commitC = sut.Commit(updateNameOther.Instance, signature, message); // C
            var loaded = loader.LoadFrom<Instance>(GetRepositoryDescription());
            var merge = loaded.Merge("newBranch");
            var chunk = merge.ModifiedChunks.Single();
            chunk.Resolve(JToken.FromObject("merged name"));
            var mergeCommit = merge.Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(GetRepositoryDescription())
                .Compare(typeof(Instance), commitC.Id, mergeCommit);
            Assert.That(changes.Modified, Has.Count.EqualTo(1));
            Assert.That(changes.Modified[0].Old.Name, Is.EqualTo("yet again modified name"));
            Assert.That(changes.Modified[0].New.Name, Is.EqualTo("merged name"));
        }
    }
}
