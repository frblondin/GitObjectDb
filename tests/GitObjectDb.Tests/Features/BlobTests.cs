using GitObjectDb.Models;
using GitObjectDb.Models.Rebase;
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
    public class BlobTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public void BlobSerializedAsNestedFile(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);

            // Assert
            sut.Execute(r =>
            {
                var blob = (Blob)r.Head[$"blob{FileSystemStorage.BlobExtension}"].Target;
                Assert.AreEqual(sut.Blob.Value, blob.GetContentText());
            });
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public void ResolveDiffsBlobUpdate(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var modified = sut.With(sut, r => r.Blob, new StringBlob("z\nb\nz"));
            var commit = container.Commit(modified.Repository, signature, message);

            // Act
            var changes = computeTreeChangesFactory(container, sut.RepositoryDescription)
                .Compare(sut.CommitId, commit.CommitId)
                .SkipIndexChanges();

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Modified));
            Assert.That(((BlobRepository)changes[0].Old).Blob, Is.EqualTo(sut.Blob));
            Assert.That(((BlobRepository)changes[0].New).Blob, Is.EqualTo(modified.Blob));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public void RebaseBlobConflictsCanResolvedAndContinued(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // Act
            CreateConflictingChange(sut, container, signature, message);
            var rebase = container.Rebase(sut.Id, "master");
            rebase.ModifiedProperties.Single(c => c.IsInConflict).Resolve(new StringBlob("y\nb\nd"));
            rebase.Continue();

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            Assert.That(container.Repositories.Single().Blob, Is.EqualTo(new StringBlob("y\nb\nd")));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public void MergeBlobConflictsCanResolvedAndContinued(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // Act
            CreateConflictingChange(sut, container, signature, message);
            var merge = container.Merge(sut.Id, "master");
            merge.ModifiedProperties.Single(c => c.IsInConflict).Resolve(new StringBlob("y\nb\nd"));
            merge.Apply(signature);

            // Assert
            Assert.That(container.Repositories.Single().Blob, Is.EqualTo(new StringBlob("y\nb\nd")));
        }

        static void CreateConflictingChange(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \    (B & C change same value)
            // newBranch:   C   ->   A---B---C
            var a = container.AddRepository(sut, signature, message); // A
            var updateName = a.With(a, r => r.Blob, new StringBlob("x\nb\nc"));
            container.Commit(updateName.Repository, signature, message); // B
            container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
            var updates = a.With(a, r => r.Blob, new StringBlob("z\nb\nd"));
            container.Commit(updates, signature, message);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public void IndexUpdateWhenBlobIsBeingChanged(IServiceProvider serviceProvider, BlobRepository repository, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message, string name)
        {
            // Arrange
            repository = repository.With(c => c
                .Add(repository, r => r.Indexes, new Index(serviceProvider, UniqueId.CreateNew(), name, nameof(Car.Blob))));
            repository = container.AddRepository(repository, signature, message);
            IndexTests.ComputeKeysCalls.Clear();

            // Act
            var modified = repository.With(repository, r => r.Blob, new StringBlob("modified blob"));
            container.Commit(modified.Repository, signature, message);

            // Assert
            Assert.That(IndexTests.ComputeKeysCalls, Has.Exactly(2).Items); // Two calls to ComputeKeys for before/after
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public void ObjectDeletionIsAlsoDeletingBlobs(BlobRepository repository, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // Arrange
            var a = container.AddRepository(repository, signature, message);

            // Act
            var b = container.Commit(
                repository.With(c => c.Remove(repository, r => r.Cars, repository.Cars[0])),
                signature,
                message);

            // Assert
            b.Execute(r =>
            {
                var changes = r.Diff.Compare<TreeChanges>(
                    r.Lookup<Commit>(a.CommitId).Tree,
                    r.Lookup<Commit>(b.CommitId).Tree);
                Assert.That(
                    changes.Deleted.Select(c => c.Path),
                    Is.EquivalentTo(new[]
                    {
                        $"Cars/{a.Cars[0].Id}/blob{FileSystemStorage.BlobExtension}",
                        $"Cars/{a.Cars[0].Id}/{FileSystemStorage.DataFile}"
                    }));
            });
        }
    }
}
