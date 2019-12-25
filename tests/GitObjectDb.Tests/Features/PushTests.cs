using GitObjectDb.Models;
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
    public class PushTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task PushNewRemoteAsync(ObjectRepository sample, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            var (tempPath, repository) = await PushNewRemoteImplAsync(sample, container, signature, message).ConfigureAwait(false);

            // Assert
            using (var repo = new Repository(tempPath))
            {
                var lastCommit = repo.Branches["master"].Tip;
                Assert.That(lastCommit.Id, Is.EqualTo(repository.CommitId));
            }
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task PushExistingRemoteAsync(ObjectRepository sample, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Arrange
            var (tempPath, repository) = await PushNewRemoteImplAsync(sample, container, signature, message).ConfigureAwait(false);
            var change = repository.WithAsync((await (await repository.Applications)[0].Pages)[0], p => p.Description, "bar");
            repository = await container.CommitAsync(change.Repository, signature, message).ConfigureAwait(false);

            // Act
            await container.PushAsync(repository.Id).ConfigureAwait(false);

            // Assert
            using (var repo = new Repository(tempPath))
            {
                var lastCommit = repo.Branches["master"].Tip;
                Assert.That(lastCommit.Id, Is.EqualTo(repository.CommitId));
            }
        }

        static async Task<(string, ObjectRepository)> PushNewRemoteImplAsync(ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            Repository.Init(tempPath, isBare: true);
            repository = await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);
            await repository.ExecuteAsync(r => r.Network.Remotes.Add("origin", tempPath)).ConfigureAwait(false);

            var change = repository.WithAsync((await (await repository.Applications)[0].Pages)[0], p => p.Description, "foo");
            repository = await container.CommitAsync(change.Repository, signature, message).ConfigureAwait(false);

            await container.PushAsync(repository.Id, "origin").ConfigureAwait(false);
            return (tempPath, repository);
        }
    }
}
