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
        public void PushNewRemote(ObjectRepository sample, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            var (tempPath, repository) = PushNewRemoteImpl(sample, container, signature, message);

            // Assert
            using (var repo = new Repository(tempPath))
            {
                var lastCommit = repo.Branches["master"].Tip;
                Assert.That(lastCommit.Id, Is.EqualTo(repository.CommitId));
            }
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void PushExistingRemote(ObjectRepository sample, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Arrange
            var (tempPath, repository) = PushNewRemoteImpl(sample, container, signature, message);
            var change = repository.Applications[0].Pages[0].With(a => a.Description == "bar");
            repository = container.Commit(change.Repository, signature, message);

            // Act
            container.Push(repository);

            // Assert
            using (var repo = new Repository(tempPath))
            {
                var lastCommit = repo.Branches["master"].Tip;
                Assert.That(lastCommit.Id, Is.EqualTo(repository.CommitId));
            }
        }

        static (string, ObjectRepository) PushNewRemoteImpl(ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            var tempPath = RepositoryFixture.GetRepositoryPath(UniqueId.CreateNew().ToString());
            Repository.Init(tempPath, isBare: true);
            repository = container.AddRepository(repository, signature, message);
            repository.RepositoryProvider.Execute(repository.RepositoryDescription, r => r.Network.Remotes.Add("origin", tempPath));

            var change = repository.Applications[0].Pages[0].With(a => a.Description == "foo");
            repository = container.Commit(change.Repository, signature, message);

            container.Push(repository, "origin");
            return (tempPath, repository);
        }
    }
}
