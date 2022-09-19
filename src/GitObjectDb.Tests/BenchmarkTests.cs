using AutoFixture;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;
using System;

namespace GitObjectDb.Tests
{
    public class BenchmarkTests
    {
        [Ignore("Only used to create large repository. Quite long, normal as we want the load time to be short not necessarily the creation time.")]
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
        public void CreateLargeSoftwareRepository(IFixture fixture)
        {
            // Arrange
            DirectoryUtils.Delete(GitObjectDbFixture.SoftwareBenchmarkRepositoryPath, false);
            fixture.Customize(new SoftwareBenchmarkCustomization());

            // Act
            using var connection = fixture.Create<IConnection>();

            // Assert
            // No assertion, the goal of this test is to create a repository to update Assets\Benchmark.zip
            Console.WriteLine($"Repository created at '{GitObjectDbFixture.SoftwareBenchmarkRepositoryPath}'.");
        }
    }
}
