using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Models.Software;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System;
using System.Linq;

namespace GitObjectDb.Tests.Queries
{
    [Parallelizable(ParallelScope.All)]
    public class ResourceQueryTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void GetNodeResources(IConnection connection, Table table)
        {
            // Act
            var result = connection.GetResources(table).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(SoftwareBenchmarkCustomization.DefaultResourcePerTableCount));
            Assert.That(result[0].NodePath, Is.EqualTo(table.Path));
            Assert.That(result[0].Path, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void ThrowsExceptionForUnattachedNode(IConnection connection)
        {
            // Arrange
            var unattachedTable = new Table(UniqueId.CreateNew());

            // Act, Assert
            Assert.Throws<ArgumentNullException>(() => connection.GetResources(unattachedTable));
        }
    }
}
