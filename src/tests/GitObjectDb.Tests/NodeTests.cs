using AutoFixture;
using FakeItEasy;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;
using System.Threading.Tasks;

namespace GitObjectDb.Tests;

public class NodeTests
{
    [Test]
    public void ToStringReturnsIdSha()
    {
        // Arrange
        var sut = A.Fake<Node>(o => o.CallsBaseMethods()); // Fake it since Node is abstract

        // Assert
        Assert.That(sut.ToString(), Is.EqualTo(sut.Id.ToString()));
    }

    [Test]
    public async Task LoadedNodesHaveTreeId()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var table = fixture.Create<Table>();

        // Assert
        Assert.That(table.TreeId, Is.Not.Null);
    }

    [Test]
    public async Task CopyConstructorSkipsTreeId()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var table = fixture.Create<Table>();

        // Act
        var copy = table with { };

        // Assert
        Assert.That(copy.TreeId, Is.Null);
    }
}
