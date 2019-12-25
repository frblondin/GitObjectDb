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
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Features
{
    public class InitializationTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task CloneRepositoryAsync(IObjectRepositoryContainer<ObjectRepository> container)
        {
            // Act
            var loaded = await container.CloneAsync(RepositoryFixture.SmallRepositoryPath).ConfigureAwait(false);

            // Assert
            Assert.That(loaded.Applications, Has.Count.EqualTo(2));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task CreateAndLoadRepositoryAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange
            sut = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false);

            // Act
            var newContainer = await containerFactory.CreateAsync<ObjectRepository>(container.Path).ConfigureAwait(false);
            var loaded = newContainer.Repositories.Single();

            // Assert
            PAssert.IsTrue(AreFunctionnally.Equivalent<ObjectRepository>(() => sut == loaded));
            foreach (var apps in (await sut.Applications).OrderBy(v => v.Id).Zip((await loaded.Applications).OrderBy(v => v.Id), (a, b) => new { source = a, desctination = b }))
            {
                PAssert.IsTrue(AreFunctionnally.Equivalent<Application>(() => apps.source == apps.desctination));
            }
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task CommitPageNameUpdateAsync(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Act
            sut = await container.AddRepositoryAsync(sut, signature, message).ConfigureAwait(false);
            var modifiedPage = sut.WithAsync((await (await sut.Applications)[0].Pages)[0], p => p.Name, "modified");
            var updated = await container.CommitAsync(modifiedPage.Repository, signature, message).ConfigureAwait(false);
            var retrievedPage = updated.FlattenAsync().OfType<Page>().FirstOrDefault(p => p.Name == "modified");

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
            var resolved = sut.TryGetFromGitPathAsync($"Applications/{application.Id}/Pages/{page.Id}/Fields/{field.Id}");

            // Assert
            Assert.That(resolved, Is.SameAs(field));
        }
    }
}
