using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Git.Backends;
using LibGit2Sharp;
using NUnit.Framework;
using PowerAssert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Features
{
    public class InitializationTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CloneRepository(IObjectRepositoryContainer<ObjectRepository> container)
        {
            // Act
            var loaded = container.Clone(RepositoryFixture.SmallRepositoryPath);

            // Assert
            Assert.That(loaded.Applications, Has.Count.EqualTo(2));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CreateAndLoadRepository(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);

            // Act
            var newContainer = containerFactory.Create<ObjectRepository>(container.Path);
            var loaded = newContainer.Repositories.Single();

            // Assert
            PAssert.IsTrue(AreFunctionnally.Equivalent<ObjectRepository>(() => sut == loaded));
            foreach (var apps in sut.Applications.OrderBy(v => v.Id).Zip(loaded.Applications.OrderBy(v => v.Id), (a, b) => new { source = a, desctination = b }))
            {
                PAssert.IsTrue(AreFunctionnally.Equivalent<Application>(() => apps.source == apps.desctination));
            }
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CommitPageNameUpdate(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            sut = container.AddRepository(sut, signature, message);
            var modifiedPage = sut.With(sut.Applications[0].Pages[0], p => p.Name, "modified");
            var updated = container.Commit(modifiedPage.Repository, signature, message);
            var retrievedPage = updated.Flatten().OfType<Page>().FirstOrDefault(p => p.Name == "modified");

            // Assert
            Assert.That(retrievedPage.Name, Is.EqualTo("modified"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void GetFromGitPath(ObjectRepository sut, Field field)
        {
            // Arrange
            var application = field.Parents().OfType<Application>().Single();
            var page = field.Parents().OfType<Page>().Single();

            // Act
            var resolved = sut.TryGetFromGitPath($"Applications/{application.Id}/Pages/{page.Id}/Fields/{field.Id}");

            // Assert
            Assert.That(resolved, Is.SameAs(field));
        }
    }
}
