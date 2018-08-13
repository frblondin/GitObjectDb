using AutoFixture;
using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public class Benchmarks
    {
        [Ignore("Only used to create large repository. Quite long, normal as we want the load time to be short not necessarily the creation time.")]
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization))]
        public void CreateLargeRepository(IFixture fixture, Signature signature, string message)
        {
            // Arrange
            var targetDir = RepositoryFixture.GetRepositoryPath("Benchmark");
            DirectoryUtils.Delete(targetDir);
            fixture.Customize(new MetadataCustomization(2, 200, 30));

            // Act
            var sut = fixture.Create<ObjectRepository>();
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GetRepositoryDescription(targetDir));

            // Assert
            // No assertion, the goal of this test is to create a repository to update Assets\Benchmark.zip
            Console.WriteLine($"Repository created at '{targetDir}'.");
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization))]
        public void LoadLargeRepository(IObjectRepositoryLoader loader)
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            loader.LoadFrom<ObjectRepository>(RepositoryFixture.BenchmarkRepositoryDescription);

            // Assert
            // Child loading is lazy so root load time should be really short
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(300)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization))]
        public void SearchInLargeRepository(IObjectRepositoryLoader loader)
        {
            // Arrange
            var sut = loader.LoadFrom<ObjectRepository>(RepositoryFixture.BenchmarkRepositoryDescription);
            var stopwatch = Stopwatch.StartNew();

            // Act
            sut.Flatten().LastOrDefault(o => o.Children.Any()); // Dummy search

            // Assert
            // Child loading is lazy so root load time should be really short
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromMinutes(1)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization))]
        public void ComputeChangesInLargeRepository(IObjectRepositoryLoader loader, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory)
        {
            // Arrange
            var sut = loader.LoadFrom<ObjectRepository>(RepositoryFixture.BenchmarkRepositoryDescription);
            var fieldToModify = sut
                .Applications.PickRandom()
                .Pages.PickRandom()
                .Fields.PickRandom();
            var computeTreeChanges = computeTreeChangesFactory(RepositoryFixture.BenchmarkRepositoryDescription);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var modifiedField = fieldToModify.With(f => f.Name == "modified");
            computeTreeChanges.Compare(sut, modifiedField.Repository);

            // Assert
            // Child loading is lazy so root load time should be really short
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(300)));
        }
    }
}
