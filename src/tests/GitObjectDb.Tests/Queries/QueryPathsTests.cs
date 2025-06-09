using AutoFixture;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Queries;

public class QueryPathsTests
{
    [Test]
    public async Task RootNodes()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.GetPathsAsync(tip).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Exactly(SoftwareBenchmarkCustomization.DefaultApplicationCount).Items);
    }

    [Test]
    public async Task TablesInApplication()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();
        var application = fixture.Create<Application>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.GetPathsAsync(tip, parentPath: application.Path).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Exactly(SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount).Items);
    }

    [Test]
    public async Task FieldsInApplicationRecursively()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();
        var application = fixture.Create<Application>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.GetPathsAsync<Field>(tip, parentPath: application.Path, isRecursive: true).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Exactly(SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount * SoftwareBenchmarkCustomization.DefaultFieldPerTableCount).Items);
    }

    [Test]
    public async Task ResourcesInTable()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();
        var table = fixture.Create<Table>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.GetPathsAsync<Resource>(tip, parentPath: table.Path, isRecursive: true).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Exactly(SoftwareBenchmarkCustomization.DefaultResourcePerTableCount).Items);
    }
}
