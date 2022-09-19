using AutoFixture.NUnit3;
using FakeItEasy;
using NUnit.Framework;

namespace GitObjectDb.Tests
{
    public class NodeTests : DisposeArguments
    {
        [Test]
        [AutoData]
        public void ToStringReturnsIdSha()
        {
            // Arrange
            var sut = A.Fake<Node>(o => o.CallsBaseMethods()); // Fake it since Node is abstract

            // Assert
            Assert.That(sut.ToString(), Is.EqualTo(sut.Id.ToString()));
        }
    }
}
