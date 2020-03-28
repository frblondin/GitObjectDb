using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Models.Software;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests.Queries
{
    [Parallelizable(ParallelScope.All)]
    public class LinqNodesTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void RootNodes(IConnection connection)
        {
            // Act
            var result = connection.AsQueryable().OfType<Application>().ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(SoftwareBenchmarkCustomization.DefaultApplicationCount));
            Assert.That(result[0].Path, Is.Not.Null);
            Assert.That(result[0].Name, Is.Not.Null);
            Assert.That(result[0].Description, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void TablesInApplication(IConnection connection, Application application)
        {
            // Act
            var result = connection.AsQueryable(application).OfType<Table>().ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount));
            Assert.That(result[0].Path, Is.Not.Null);
            Assert.That(result[0].Name, Is.Not.Null);
            Assert.That(result[0].Description, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void FilterById(IConnection connection, Field field)
        {
            // Act
            var result = (from f in connection.AsQueryable(isRecursive: true)
                          where f.Id == field.Id
                          select f.Id).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void FilterByPath(IConnection connection, Field field)
        {
            // Act
            var result = (from f in connection.AsQueryable(isRecursive: true)
                          where f.Path == field.Path
                          select f.Id).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void OfType(IConnection connection)
        {
            // Act
            var result = (from f in connection.AsQueryable(isRecursive: true).OfType<Field>()
                          select f.Id).ToList();

            // Assert
            var expected = SoftwareBenchmarkCustomization.DefaultApplicationCount *
                SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount *
                SoftwareBenchmarkCustomization.DefaultFieldPerTableCount;
            Assert.That(result, Has.Count.EqualTo(expected));
        }
    }
}
