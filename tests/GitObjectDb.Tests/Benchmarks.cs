using AutoFixture;
using GitObjectDb.Git;
using GitObjectDb.IO;
using GitObjectDb.Models;
using GitObjectDb.Services;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Git.Backends;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests
{
    [Parallelizable(ParallelScope.All)]
    public class Benchmarks
    {
        [Ignore("Only used to create large repository. Quite long, normal as we want the load time to be short not necessarily the creation time.")]
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization))]
        public void CreateLargeRepository(IFixture fixture, Signature signature, string message)
        {
            // Arrange
            DirectoryUtils.Delete(RepositoryFixture.BenchmarkRepositoryPath, false);
            fixture.Customize(new ModelCustomization(2, 200, 30, RepositoryFixture.BenchmarkRepositoryPath));

            // Act
            var container = fixture.Create<IObjectRepositoryContainer<ObjectRepository>>();
            var repository = fixture.Create<ObjectRepository>();
            container.AddRepository(repository, signature, message);

            // Assert
            // No assertion, the goal of this test is to create a repository to update Assets\Benchmark.zip
            Console.WriteLine($"Repository created at '{RepositoryFixture.BenchmarkRepositoryPath}'.");
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LoadLargeRepository(ObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            loader.LoadFrom(container, RepositoryFixture.BenchmarkRepositoryDescription);

            // Assert
            // Child loading is lazy so root load time should be really short
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(300)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void SearchInLargeRepository(IObjectRepositorySearch search, ObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader)
        {
            // Arrange
            var sut = loader.LoadFrom(container, RepositoryFixture.BenchmarkRepositoryDescription);
            var stopwatch = Stopwatch.StartNew();
            var page = sut.Applications.PickRandom().Pages.PickRandom();

            // Act
            var result = search.Grep(sut, page.Id.ToString());
            stopwatch.Stop();
            Console.WriteLine($"Grep total duration: {stopwatch.Elapsed}");

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void FullLoadInLargeRepository(ObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader)
        {
            // Arrange
            var sut = loader.LoadFrom(container, RepositoryFixture.BenchmarkRepositoryDescription);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = sut.Flatten().Last();
            stopwatch.Stop();
            Console.WriteLine($"Full load total duration: {stopwatch.Elapsed}");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void SearchInLargeRepositoryUsingLightDbBackend(IObjectRepositorySearch search, ObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader)
        {
            // Arrange
            var dbFile = Path.Combine(RepositoryFixture.BenchmarkRepositoryDescription.Path, "lite.db");
            LiteDbBackend backend = default;
            var repositoryDescription = RepositoryFixture.BenchmarkRepositoryDescription
                                                         .WithBackend(() => backend = new LiteDbBackend($"filename={dbFile}; journal=false"));
            var sut = loader.LoadFrom(container, repositoryDescription);
            sut.Execute(r => r.ObjectDatabase.CopyAllBlobs(backend));
            var stopwatch = Stopwatch.StartNew();
            var page = sut.Applications.PickRandom().Pages.PickRandom();

            // Act
            var result = search.Grep(sut, page.Id.ToString());
            stopwatch.Stop();
            Console.WriteLine($"Grep total duration: {stopwatch.Elapsed}");

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ComputeChangesInLargeRepository(ObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            var sut = loader.LoadFrom(container, RepositoryFixture.BenchmarkRepositoryDescription);
            var fieldToModify = sut.Flatten().OfType<Field>().First(
                f => f.Content.MatchOrDefault(matchLink: l => true));
            var computeTreeChanges = computeTreeChangesFactory(container, RepositoryFixture.BenchmarkRepositoryDescription);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var modifiedField = sut.With(fieldToModify, f => f.Name, "modified");
            computeTreeChanges.Compare(sut, modifiedField.Repository);

            // Assert
            // Child loading is lazy so root load time should be really short
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(300)));
        }
    }
}
