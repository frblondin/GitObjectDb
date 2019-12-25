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
    public class FetchTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task FetchAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange
            sut = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false);
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            var clientContainer = await containerFactory.CreateAsync<ObjectRepository>(tempPath).ConfigureAwait(false);
            await clientContainer.CloneAsync(container.Repositories.Single().RepositoryDescription.Path).ConfigureAwait(false);

            // Arrange - Update source repository
            var change = sut.WithAsync((await (await sut.Applications)[0].Pages)[0], p => p.Description, "foo");
            var commitResult = await container.CommitAsync(change.Repository, signature, message).ConfigureAwait(false);

            // Act
            var fetchResult = await clientContainer.FetchAsync(clientContainer.Repositories.Single().Id).ConfigureAwait(false);

            // Assert
            Assert.That(fetchResult.CommitId, Is.EqualTo(commitResult.CommitId));
            Assert.That(clientContainer.Repositories.Single().CommitId, Is.EqualTo(sut.CommitId));
        }
    }
}
