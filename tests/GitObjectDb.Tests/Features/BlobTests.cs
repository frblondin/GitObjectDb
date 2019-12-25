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
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Features
{
    public class BlobTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public async Task BlobSerializedAsNestedFileAsync(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // Arrange
            sut = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false);

            // Assert
            await sut.ExecuteAsync(r =>
            {
                var blob = (Blob)r.Head[$"blob{FileSystemStorage.BlobExtension}"].Target;
                Assert.AreEqual(sut.Blob.Value, blob.GetContentText());
            }).ConfigureAwait(false);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public async Task ResolveDiffsBlobUpdateAsync(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            sut = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false);
            var modified = sut.WithAsync(sut, r => r.Blob, new StringBlob("z\nb\nz"));
            var commit = await container.CommitAsync(modified.Repository, signature, message).ConfigureAwait(false);

            // Act
            var changes = (await computeTreeChangesFactory(container, sut.RepositoryDescription)
                .CompareAsync(sut.CommitId, commit.CommitId).ConfigureAwait(false))
                .SkipIndexChanges();

            // Assert
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes[0].Status, Is.EqualTo(ChangeKind.Modified));
            Assert.That(((BlobRepository)changes[0].Old).Blob, Is.EqualTo(sut.Blob));
            Assert.That(((BlobRepository)changes[0].New).Blob, Is.EqualTo(modified.Blob));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public async Task RebaseBlobConflictsCanResolvedAndContinuedAsync(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // Act
            await CreateConflictingChangeAsync(sut, container, signature, message).ConfigureAwait(false);
            var rebase = await container.RebaseAsync(sut.Id, "master").ConfigureAwait(false);
            rebase.ModifiedProperties.Single(c => c.IsInConflict).Resolve(new StringBlob("y\nb\nd"));
            await rebase.ContinueAsync().ConfigureAwait(false);

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            Assert.That(container.Repositories.Single().Blob, Is.EqualTo(new StringBlob("y\nb\nd")));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public async Task MergeBlobConflictsCanResolvedAndContinuedAsync(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // Act
            await CreateConflictingChangeAsync(sut, container, signature, message).ConfigureAwait(false);
            var merge = await container.MergeAsync(sut.Id, "master").ConfigureAwait(false);
            merge.ModifiedProperties.Single(c => c.IsInConflict).Resolve(new StringBlob("y\nb\nd"));
            await merge.ApplyAsync(signature).ConfigureAwait(false);

            // Assert
            Assert.That(container.Repositories.Single().Blob, Is.EqualTo(new StringBlob("y\nb\nd")));
        }

        static async Task CreateConflictingChangeAsync(BlobRepository sut, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // master:    A---B
            //             \    (B & C change same value)
            // newBranch:   C   ->   A---B---C
            var a = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false); // A
            var updateName = a.WithAsync(a, r => r.Blob, new StringBlob("x\nb\nc"));
            await container.CommitAsync(updateName.Repository, signature, message).ConfigureAwait(false); // B
            await container.CheckoutAsync(a.Id, "newBranch", createNewBranch: true, "HEAD~1").ConfigureAwait(false);
            var updates = a.WithAsync(a, r => r.Blob, new StringBlob("z\nb\nd"));
            await container.CommitAsync(updates, signature, message).ConfigureAwait(false);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public async Task IndexUpdateWhenBlobIsBeingChangedAsync(IServiceProvider serviceProvider, BlobRepository repository, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message, string name)
        {
            // Arrange
            repository = repository.With(c => c
                .Add(repository, r => r.Indexes, new Index(serviceProvider, UniqueId.CreateNew(), name, nameof(Car.Blob))));
            repository = await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);
            IndexTests.ComputeKeysCalls.Clear();

            // Act
            var modified = repository.WithAsync(repository, r => r.Blob, new StringBlob("modified blob"));
            await container.CommitAsync(modified.Repository, signature, message).ConfigureAwait(false);

            // Assert
            Assert.That(IndexTests.ComputeKeysCalls, Has.Count.EqualTo(2)); // Two calls to ComputeKeys for before/after
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(BlobCustomization))]
        public async Task ObjectDeletionIsAlsoDeletingBlobsAsync(BlobRepository repository, IObjectRepositoryContainer<BlobRepository> container, Signature signature, string message)
        {
            // Arrange
            var a = await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);
            var firstCar = (await repository.Cars)[0];

            // Act
            var b = await container.CommitAsync(
                repository.With(c => c.Remove(repository, r => r.Cars, firstCar)),
                signature,
                message).ConfigureAwait(false);

            // Assert
            firstCar = (await a.Cars)[0];
            await b.ExecuteAsync(r =>
            {
                var changes = r.Diff.Compare<TreeChanges>(
                    r.Lookup<Commit>(a.CommitId).Tree,
                    r.Lookup<Commit>(b.CommitId).Tree);
                Assert.That(
                    changes.Deleted.Select(c => c.Path),
                    Is.EquivalentTo(new[]
                    {
                        $"Cars/{firstCar.Id}/blob{FileSystemStorage.BlobExtension}",
                        $"Cars/{firstCar.Id}/{FileSystemStorage.DataFile}"
                    }));
            }).ConfigureAwait(false);
        }
    }
}
