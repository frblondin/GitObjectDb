using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using Models.Software;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests.Queries;

[Parallelizable(ParallelScope.Self | ParallelScope.Children)]
public class QueryNodesTests : DisposeArguments
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
    public void RootNodes(IConnection connection)
    {
        // Act
        var result = connection.GetNodes<Application>("main").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(SoftwareBenchmarkCustomization.DefaultApplicationCount));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Path, Is.Not.Null);
            Assert.That(result[0].Name, Is.Not.Null);
            Assert.That(result[0].Description, Is.Not.Null);
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
    public void TablesInApplication(IConnection connection, Application application)
    {
        // Act
        var result = connection.GetNodes<Table>("main", parent: application).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Path, Is.Not.Null);
            Assert.That(result[0].Name, Is.Not.Null);
            Assert.That(result[0].Description, Is.Not.Null);
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
    public void OfType(IConnection connection)
    {
        // Act
        var result = (from f in connection.GetNodes<Field>("main", isRecursive: true)
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

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
    public void LookupByPass(IConnection connection, Table table)
    {
        // Act
        var result = connection.Lookup<Table>("main", table.Path);

        // Assert
        Assert.That(result, Is.EqualTo(table));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareBenchmarkCustomization))]
    public void LookupById(IConnection connection, Table table)
    {
        // Act
        var result = connection.Lookup<Table>("main", table.Path);

        // Assert
        Assert.That(result, Is.EqualTo(table));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void ModifiedReferenceEditedInIndexGetsResolved(IConnection connection, Field field, string newDescription)
    {
        // Arrange
        var index = connection.GetIndex("main",
            c => c.CreateOrUpdate(field.LinkedTable with { Description = newDescription }));

        // Act
        var resolvedField = index.TryLoadItem<Field>(field.Path);

        // Assert
        Assert.That(resolvedField.LinkedTable.Description, Is.EqualTo(newDescription));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void TryLoadItemGetsItemsFromIndex(IConnection connection, Field field, string newDescription)
    {
        // Arrange
        var index = connection.GetIndex("main",
            c => c.CreateOrUpdate(field.LinkedTable with { Description = newDescription }));

        // Act
        var resolvedTable = index.TryLoadItem<Table>(field.LinkedTable.Path);

        // Assert
        Assert.That(resolvedTable.Description, Is.EqualTo(newDescription));
    }
}
