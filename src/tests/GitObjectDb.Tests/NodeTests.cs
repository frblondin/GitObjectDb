using FakeItEasy;
using GitObjectDb.Tests.Assets.Tools;
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
}
