using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using Models.Software;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests.Queries
{
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class QueryNodesTests : DisposeArguments
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void RootNodes(IConnection connection)
        {
            // Act
            var result = connection.GetNodes<Application>().ToList();

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
            var result = connection.GetNodes<Table>(application).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount));
            Assert.That(result[0].Path, Is.Not.Null);
            Assert.That(result[0].Name, Is.Not.Null);
            Assert.That(result[0].Description, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void OfType(IConnection connection)
        {
            // Act
            var result = (from f in connection.GetNodes<Field>(isRecursive: true)
                          select f.Id).ToList();

            // Assert
            var expected = SoftwareBenchmarkCustomization.DefaultApplicationCount *
                SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount *
                SoftwareBenchmarkCustomization.DefaultFieldPerTableCount;
            Assert.That(result, Has.Count.EqualTo(expected));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
        public void EmbeddedResourceGetsLoaded(Constant constant)
        {
            // Assert
            Assert.That(constant.EmbeddedResource, Is.Not.Null);
        }
    }
}
