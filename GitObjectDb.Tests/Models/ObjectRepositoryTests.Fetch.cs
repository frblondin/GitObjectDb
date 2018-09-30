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

namespace GitObjectDb.Tests.Models
{
    public partial class ObjectRepositoryTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void Fetch(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, Signature signature, string message)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var tempPath = RepositoryFixture.GetRepositoryPath(UniqueId.CreateNew().ToString());
            var clientContainer = new ObjectRepositoryContainer<ObjectRepository>(serviceProvider, tempPath);
            clientContainer.Clone(container.Repositories.Single().RepositoryDescription.Path);

            // Arrange - Update source repository
            var change = sut.Applications[0].Pages[0].With(a => a.Description == "foo");
            var commitResult = container.Commit(change.Repository, signature, message);

            // Act
            var fetchResult = clientContainer.Fetch(clientContainer.Repositories.Single());

            // Assert
            Assert.That(fetchResult.CommitId, Is.EqualTo(commitResult.CommitId));
            Assert.That(clientContainer.Repositories.Single().CommitId, Is.EqualTo(sut.CommitId));
        }
    }
}
