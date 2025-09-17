using AutoFixture;
using NUnit.Framework;
using System;

namespace GitObjectDb.Tests;

public class UniqueIdTests
{
    [Test]
#pragma warning disable NUnit2009 // The same value has been provided as both the actual and the expected argument
    public void OperatorEquality()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var sha = fixture.Create<string>();

        // Act, Assert
        Assert.That(new UniqueId(sha), Is.EqualTo(new UniqueId(sha)));
    }
#pragma warning restore NUnit2009 // The same value has been provided as both the actual and the expected argument

    [Test]
    public void OperatorInequality()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var sha1 = fixture.Create<string>();
        var sha2 = fixture.Create<string>();

        // Act, Assert
        Assert.That(new UniqueId(sha1), Is.Not.EqualTo(new UniqueId(sha2)));
    }

    [Test]
    public void OperatorLowerThan()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var sha1 = fixture.Create<string>();
        var sha2 = fixture.Create<string>();

        // Act, Assert
        Assert.That(
            string.CompareOrdinal(sha1, sha2) < 0, Is.EqualTo(new UniqueId(sha1) < new UniqueId(sha2)));
    }

    [Test]
    public void OperatorLowerThanOrEqual()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var sha1 = fixture.Create<string>();
        var sha2 = fixture.Create<string>();

        // Act, Assert
        Assert.That(
            string.CompareOrdinal(sha1, sha2) <= 0, Is.EqualTo(new UniqueId(sha1) <= new UniqueId(sha2)));
    }

    [Test]
    public void OperatorGreaterThan()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var sha1 = fixture.Create<string>();
        var sha2 = fixture.Create<string>();

        // Act, Assert
        Assert.That(
            string.CompareOrdinal(sha1, sha2) > 0, Is.EqualTo(new UniqueId(sha1) > new UniqueId(sha2)));
    }

    [Test]
    public void OperatorGreaterThanOrEqual()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var sha1 = fixture.Create<string>();
        var sha2 = fixture.Create<string>();

        // Act, Assert
        Assert.That(
            string.CompareOrdinal(sha1, sha2) >= 0, Is.EqualTo(new UniqueId(sha1) >= new UniqueId(sha2)));
    }

    [Test]
    public void TryParseFailsWhenEmpty()
    {
        // Act, Assert
        Assert.That(
            UniqueId.TryParse(string.Empty, out var _),
            Is.False);
    }

    [Test]
    public void EqualsBoxed()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var sha = fixture.Create<string>();
        var boxed = (object)new UniqueId(sha);

        // Act, Assert
        Assert.That(new UniqueId(sha), Is.EqualTo(boxed));
    }

    private class Customization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register(() => Guid.NewGuid().ToString("N"));
        }
    }
}
