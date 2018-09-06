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

namespace GitObjectDb.Tests.Models
{
    public partial class ObjectRepositoryTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void CloneRepository(IObjectRepositoryContainer<ObjectRepository> container)
        {
            // Act
            var loaded = container.Clone(RepositoryFixture.SmallRepositoryPath);

            // Assert
            Assert.That(loaded.Applications, Has.Count.EqualTo(2));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void CreateAndLoadRepository(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, Signature signature, string message)
        {
            // Arrange
            container.AddRepository(sut, signature, message);

            // Act
            var newContainer = new ObjectRepositoryContainer<ObjectRepository>(serviceProvider, container.Path);
            var loaded = newContainer.Repositories.Single();

            // Assert
            PAssert.IsTrue(AreFunctionnally.Equivalent<ObjectRepository>(() => sut == loaded));
            foreach (var apps in sut.Applications.OrderBy(v => v.Id).Zip(loaded.Applications.OrderBy(v => v.Id), (a, b) => new { source = a, desctination = b }))
            {
                PAssert.IsTrue(AreFunctionnally.Equivalent<Application>(() => apps.source == apps.desctination));
            }
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void CommitPageNameUpdate(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message)
        {
            // Act
            container.AddRepository(sut, signature, message);
            var modifiedPage = page.With(p => p.Name == "modified");
            var updated = container.Commit(modifiedPage.Repository, signature, message);
            var retrievedPage = updated.Flatten().OfType<Page>().FirstOrDefault(p => p.Name == "modified");

            // Assert
            Assert.That(retrievedPage.Name, Is.EqualTo("modified"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
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
