using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Git.Backends;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using NUnit.Framework;
using PowerAssert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public class InstanceTests
    {
        string _path;

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void CreateAndLoadRepository(IInstanceLoader loader, Instance sut, Signature signature, string message, InMemoryBackend inMemoryBackend)
        {
            // Act
            sut.SaveInNewRepository(signature, message, _path, GetRepositoryDescription(inMemoryBackend));
            var loaded = loader.LoadFrom<Instance>(GetRepositoryDescription(inMemoryBackend), r => r.Head.Tip.Tree);

            // Assert
            PAssert.IsTrue(AreFunctionnally.Equivalent<Instance>(() => sut == loaded));
            foreach (var apps in sut.Applications.OrderBy(v => v.Id).Zip(loaded.Applications.OrderBy(v => v.Id), (a, b) => new { source = a, desctination = b }))
            {
                PAssert.IsTrue(AreFunctionnally.Equivalent<Application>(() => apps.source == apps.desctination));
            }
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void CommitPageNameUpdate(Instance sut, Page page, Signature signature, string message, InMemoryBackend inMemoryBackend)
        {
            // Act
            sut.SaveInNewRepository(signature, message, _path, GetRepositoryDescription(inMemoryBackend));
            var modifiedPage = page.With(p => p.Name == "modified");
            var commit = sut.Commit(modifiedPage.Instance, signature, message);

            // Assert
            Assert.That(commit, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsPageNameUpdate(Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            sut.SaveInNewRepository(signature, message, _path, GetRepositoryDescription(inMemoryBackend));
            var modifiedPage = page.With(p => p.Name == "modified");
            sut.Commit(modifiedPage.Instance, signature, message);

            // Act
            var changes = computeTreeChangesFactory(GetRepositoryDescription(inMemoryBackend))
                .Compare<Instance>(r => r.Head.Commits.Skip(1).First().Tree, r => r.Head.Tip.Tree);

            // Assert
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
            Assert.That(changes.Modified, Has.Count.EqualTo(1));
            Assert.That(changes.Modified[0].Old.Name, Is.EqualTo(page.Name));
            Assert.That(changes.Modified[0].New.Name, Is.EqualTo(modifiedPage.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsFieldAddition(IServiceProvider serviceProvider, Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            sut.SaveInNewRepository(signature, message, _path, GetRepositoryDescription(inMemoryBackend));
            var field = new Field(serviceProvider, Guid.NewGuid(), "foo");
            var modifiedPage = page.With(p => p.Fields.Add(field));
            sut.Commit(modifiedPage.Instance, signature, message);

            // Act
            var changes = computeTreeChangesFactory(GetRepositoryDescription(inMemoryBackend))
                .Compare<Instance>(r => r.Head.Commits.Skip(1).First().Tree, r => r.Head.Tip.Tree);

            // Assert
            Assert.That(changes.Modified, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
            Assert.That(changes.Added, Has.Count.EqualTo(1));
            Assert.That(changes.Added[0].Old, Is.Null);
            Assert.That(changes.Added[0].New.Name, Is.EqualTo(field.Name));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ResolveDiffsFieldDeletion(Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            sut.SaveInNewRepository(signature, message, _path, GetRepositoryDescription(inMemoryBackend));
            var field = page.Fields[5];
            var modifiedPage = page.With(p => p.Fields.Delete(field));
            sut.Commit(modifiedPage.Instance, signature, message);

            // Act
            var changes = computeTreeChangesFactory(GetRepositoryDescription(inMemoryBackend))
                .Compare<Instance>(r => r.Head.Commits.Skip(1).First().Tree, r => r.Head.Tip.Tree);

            // Assert
            Assert.That(changes.Modified, Is.Empty);
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Has.Count.EqualTo(1));
            Assert.That(changes.Deleted[0].New, Is.Null);
            Assert.That(changes.Deleted[0].Old.Name, Is.EqualTo(field.Name));
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

        RepositoryDescription GetRepositoryDescription(OdbBackend backend) => new RepositoryDescription(_path, backend);

        [SetUp]
        public void GetTempPath() =>
            _path = Path.Combine(Path.GetTempPath(), "Repos", Guid.NewGuid().ToString());

        [TearDown]
        public void DeleteTempPath() =>
            DirectoryUtils.Delete(_path);
    }
}
