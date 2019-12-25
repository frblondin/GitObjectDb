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
using System.Threading.Tasks;

namespace GitObjectDb.Tests
{
    [Parallelizable(ParallelScope.Children)]
    public class Benchmarks
    {
        [Ignore("Only used to create large repository. Quite long, normal as we want the load time to be short not necessarily the creation time.")]
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization))]
        public async Task CreateLargeRepositoryAsync(IFixture fixture, Signature signature, string message)
        {
            // Arrange
            DirectoryUtils.Delete(RepositoryFixture.BenchmarkRepositoryPath, false);
            fixture.Customize(new ModelCustomization(2, 200, 30, RepositoryFixture.BenchmarkRepositoryPath));

            // Act
            var container = fixture.Create<IObjectRepositoryContainer<ObjectRepository>>();
            var repository = fixture.Create<ObjectRepository>();
            await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);

            // Assert
            // No assertion, the goal of this test is to create a repository to update Assets\Benchmark.zip
            Console.WriteLine($"Repository created at '{RepositoryFixture.BenchmarkRepositoryPath}'.");
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task LoadLargeRepositoryAsync(IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            await loader.LoadFromAsync(container, RepositoryFixture.BenchmarkRepositoryDescription).ConfigureAwait(false);

            // Assert
            // Child loading is lazy so root load time should be really short
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(300)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task SearchInLargeRepositoryAsync(IObjectRepositorySearch search, IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader)
        {
            // Arrange
            var sut = await loader.LoadFromAsync(container, RepositoryFixture.BenchmarkRepositoryDescription).ConfigureAwait(false);
            var applications = await sut.Applications.ConfigureAwait(false);
            var page = (await applications.PickRandom().Pages.ConfigureAwait(false)).PickRandom();
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await search.GrepAsync(sut, page.Id.ToString()).ToListAsync();
            stopwatch.Stop();
            Console.WriteLine($"Grep total duration: {stopwatch.Elapsed}");

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task FullLoadInLargeRepositoryAsync(IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader)
        {
            // Arrange
            var sut = await loader.LoadFromAsync(container, RepositoryFixture.BenchmarkRepositoryDescription).ConfigureAwait(false);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = sut.FlattenAsync().Last();
            stopwatch.Stop();
            Console.WriteLine($"Full load total duration: {stopwatch.Elapsed}");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task SearchInLargeRepositoryUsingLightDbBackendAsync(IObjectRepositorySearch search, IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader)
        {
            // Arrange
            var dbFile = Path.Combine(RepositoryFixture.BenchmarkRepositoryDescription.Path, "lite.db");
            LiteDbBackend backend = default;
            var repositoryDescription = RepositoryFixture.BenchmarkRepositoryDescription
                                                         .WithBackend(() => backend = new LiteDbBackend($"filename={dbFile}; journal=false"));
            var sut = await loader.LoadFromAsync(container, repositoryDescription).ConfigureAwait(false);
            await sut.ExecuteAsync(r => r.ObjectDatabase.CopyAllBlobs(backend)).ConfigureAwait(false);
            var page = (await (await sut.Applications).PickRandom().Pages).PickRandom();
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await search.GrepAsync(sut, page.Id.ToString()).ToListAsync();
            stopwatch.Stop();
            Console.WriteLine($"Grep total duration: {stopwatch.Elapsed}");

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task ComputeChangesInLargeRepositoryAsync(IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader, ComputeTreeChangesFactory computeTreeChangesFactory)
        {
            // Arrange
            var sut = await loader.LoadFromAsync(container, RepositoryFixture.BenchmarkRepositoryDescription).ConfigureAwait(false);
            var fieldToModify = sut.FlattenAsync().OfType<Field>().First(
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

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task SearchInLargeRepositoryUsingIndexAsync(IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryLoader loader)
        {
            // Arrange
            var sut = await loader.LoadFromAsync(container, RepositoryFixture.BenchmarkRepositoryDescription).ConfigureAwait(false);
            var referencedPage = File.ReadAllText("Assets\\Benchmark.ReferencedPage.txt").Trim();
            var index = (await sut.Indexes).Single(i => i is LinkFieldReferrerIndex);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = index[referencedPage].ToList();
            stopwatch.Stop();
            Console.WriteLine($"Grep total duration: {stopwatch.Elapsed}");

            // Assert
            Assert.That(result, Is.Not.Empty);
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(300)));
        }
    }
}
