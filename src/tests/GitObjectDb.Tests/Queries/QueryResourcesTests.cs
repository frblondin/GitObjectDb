using AutoFixture;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Queries;

public class QueryResourcesTests
{
    [Test]
    public async Task GetNodeResources()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();
        var table = fixture.Create<Table>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.GetResourcesAsync(tip, table).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(SoftwareBenchmarkCustomization.DefaultResourcePerTableCount));
        Assert.That(result[0].Path, Is.Not.Null);
    }

    [Test]
    public async Task ThrowsExceptionForUnattachedNode()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();

        // Arrange
        var unattachedTable = new Table();

        // Act, Assert
        var tip = await connection.Repository.GetCommittishAsync("main");
        Assert.Throws<InvalidOperationException>(() => connection.GetResourcesAsync(tip, unattachedTable).ToEnumerable().ToList());
    }
}
