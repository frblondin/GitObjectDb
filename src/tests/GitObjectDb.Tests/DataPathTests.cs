using AutoFixture;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;
using System.Threading.Tasks;

namespace GitObjectDb.Tests;

public class DataPathTests
{
    [Test]
    public async Task GetParentNode()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var sut = fixture.Create<IConnection>();
        var application = fixture.Create<Application>();
        var table = fixture.Create<Table>();

        // Act, Assert
        Assert.That(table.Path.GetParentNode(sut.Serializer), Is.EqualTo(application.Path));
    }

    [Test]
    public async Task GetParentNodeForLeaves()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var sut = fixture.Create<IConnection>();
        var table = fixture.Create<Table>();
        var constant = fixture.Create<Constant>();

        // Act, Assert
        Assert.That(constant.Path.GetParentNode(sut.Serializer), Is.EqualTo(table.Path));
    }
}
