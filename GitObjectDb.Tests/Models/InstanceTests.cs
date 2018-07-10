using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Git.Backends;
using LibGit2Sharp;
using NUnit.Framework;
using PowerAssert;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    [DebuggerDisplay("{_path}")]
    public partial class InstanceTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void CreateAndLoadRepository(IInstanceLoader loader, Instance sut, Signature signature, string message, InMemoryBackend inMemoryBackend)
        {
            // Act
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, GetRepositoryDescription(inMemoryBackend));
            var loaded = loader.LoadFrom<Instance>(GetRepositoryDescription(inMemoryBackend));

            // Assert
            PAssert.IsTrue(AreFunctionnally.Equivalent<Instance>(() => sut == loaded));
            foreach (var apps in sut.Applications.OrderBy(v => v.Id).Zip(loaded.Applications.OrderBy(v => v.Id), (a, b) => new { source = a, desctination = b }))
            {
                PAssert.IsTrue(AreFunctionnally.Equivalent<Application>(() => apps.source == apps.desctination));
            }
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergePageNameUpdate(Instance sut, Page page, Signature signature, string message, InMemoryBackend inMemoryBackend)
        {
            // Act
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GitPath, GetRepositoryDescription(inMemoryBackend));
            var modifiedPage = page.With(p => p.Name == "modified");
            var commit = sut.Commit(modifiedPage.Instance, signature, message);

            // Assert
            Assert.That(commit, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void GetFromGitPath(Instance sut, Field field)
        {
            // Arrange
            var application = field.Parents().OfType<Application>().Single();
            var page = field.Parents().OfType<Page>().Single();

            // Act
            var resolved = sut.TryGetFromGitPath($"Applications/{application.Id}/Pages/{page.Id}/Fields/{field.Id}");

            // Assert
            Assert.That(resolved, Is.SameAs(field));
        }

        static RepositoryDescription GetRepositoryDescription(OdbBackend backend = null) => new RepositoryDescription(RepositoryFixture.GitPath, backend);
    }
}
