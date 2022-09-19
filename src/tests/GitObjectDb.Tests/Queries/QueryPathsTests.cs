using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using Models.Software;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests.Queries;

[Parallelizable(ParallelScope.Self | ParallelScope.Children)]
public class QueryPathsTests : DisposeArguments
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
    public void RootNodes(IConnection connection)
    {
        // Act
        var result = connection.GetPaths().ToList();

        // Assert
        Assert.That(result, Has.Exactly(SoftwareBenchmarkCustomization.DefaultApplicationCount).Items);
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
    public void TablesInApplication(IConnection connection, Application application)
    {
        // Act
        var result = connection.GetPaths(application.Path).ToList();

        // Assert
        Assert.That(result, Has.Exactly(SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount).Items);
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
    public void FieldsInApplicationRecursively(IConnection connection, Application application)
    {
        // Act
        var result = connection.GetPaths<Field>(application.Path, isRecursive: true).ToList();

        // Assert
        Assert.That(result, Has.Exactly(SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount * SoftwareBenchmarkCustomization.DefaultFieldPerTableCount).Items);
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
    public void ResourcesInTable(IConnection connection, Table table)
    {
        // Act
        var result = connection.GetPaths<Resource>(table.Path, isRecursive: true).ToList();

        // Assert
        Assert.That(result, Has.Exactly(SoftwareBenchmarkCustomization.DefaultResourcePerTableCount).Items);
    }
}
