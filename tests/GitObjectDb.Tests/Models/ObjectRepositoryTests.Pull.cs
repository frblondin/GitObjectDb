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
        public void PullNotRequiringAnyMerge(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, Signature signature, string message)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            var clientContainer = new ObjectRepositoryContainer<ObjectRepository>(serviceProvider, tempPath);
            clientContainer.Clone(container.Repositories.Single().RepositoryDescription.Path);

            // Arrange - Update source repository
            var change = sut.Applications[0].Pages[0].With(a => a.Description == "foo");
            var commitResult = container.Commit(change.Repository, signature, message);

            // Act
            var pullResult = clientContainer.Pull(clientContainer.Repositories.Single());
            pullResult.Apply(signature);

            // Assert
            ObjectRepositoryContainer.EnsureHeadCommit(clientContainer.Repositories.Single());
            Assert.That(pullResult.RequiresMergeCommit, Is.False);
            Assert.That(clientContainer.Repositories.Single().CommitId, Is.EqualTo(commitResult.CommitId));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PullRequiringMerge(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, Signature signature, string message)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            var clientContainer = new ObjectRepositoryContainer<ObjectRepository>(serviceProvider, tempPath);
            clientContainer.Clone(container.Repositories.Single().RepositoryDescription.Path);

            // Arrange - Update source repository
            var change = sut.Applications[0].Pages[0].With(a => a.Description == "foo");
            container.Commit(change.Repository, signature, message);

            // Arrange - Update client repository
            var clientChange = clientContainer.Repositories.Single().Applications[0].Pages[0].With(a => a.Name == "bar");
            clientContainer.Commit(clientChange.Repository, signature, message);

            // Act
            var pullResult = clientContainer.Pull(clientContainer.Repositories.Single());
            pullResult.Apply(signature);

            // Assert
            Assert.That(pullResult.RequiresMergeCommit, Is.True);
            Assert.That(clientContainer.Repositories.Single().Applications[0].Pages[0].Description, Is.EqualTo("foo"));
        }
    }
}
