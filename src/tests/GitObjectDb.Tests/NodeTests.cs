using FakeItEasy;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using Models.Software;
using NUnit.Framework;

namespace GitObjectDb.Tests;

public class NodeTests : DisposeArguments
{
    [Test]
    [AutoDataCustomizations]
    public void ToStringReturnsIdSha()
    {
        // Arrange
        var sut = A.Fake<Node>(o => o.CallsBaseMethods()); // Fake it since Node is abstract

        // Assert
        Assert.That(sut.ToString(), Is.EqualTo(sut.Id.ToString()));
    }

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) })]
    public void LoadedNodesHaveTreeId(Table table)
    {
        // Assert
        Assert.That(table.TreeId, Is.Not.Null);
    }

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) })]
    public void CopyConstructorSkipsTreeId(Table table)
    {
        // Act
        var copy = table with { };

        // Assert
        Assert.That(copy.TreeId, Is.Null);
    }
}
