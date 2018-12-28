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
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void Fetch(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            var clientContainer = containerFactory.Create<ObjectRepository>(tempPath);
            clientContainer.Clone(container.Repositories.Single().RepositoryDescription.Path);

            // Arrange - Update source repository
            var change = sut.Applications[0].Pages[0].With(a => a.Description == "foo");
            var commitResult = container.Commit(change.Repository, signature, message);

            // Act
            var fetchResult = clientContainer.Fetch(clientContainer.Repositories.Single().Id);

            // Assert
            Assert.That(fetchResult.CommitId, Is.EqualTo(commitResult.CommitId));
            Assert.That(clientContainer.Repositories.Single().CommitId, Is.EqualTo(sut.CommitId));
        }
    }
}
