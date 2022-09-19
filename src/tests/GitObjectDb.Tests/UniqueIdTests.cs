using AutoFixture;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System;

namespace GitObjectDb.Tests;

public class UniqueIdTests
{
    [Test]
    [AutoDataCustomizations(typeof(Customization))]
#pragma warning disable NUnit2009 // The same value has been provided as both the actual and the expected argument
    public void OperatorEquality(string sha) =>
        Assert.That(new UniqueId(sha), Is.EqualTo(new UniqueId(sha)));
#pragma warning restore NUnit2009 // The same value has been provided as both the actual and the expected argument

    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void OperatorInequality(string sha1, string sha2) =>
        Assert.That(new UniqueId(sha1), Is.Not.EqualTo(new UniqueId(sha2)));

    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void OperatorLowerThan(string sha1, string sha2) =>
        Assert.That(
            string.CompareOrdinal(sha1, sha2) < 0, Is.EqualTo(new UniqueId(sha1) < new UniqueId(sha2)));

    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void OperatorLowerThanOrEqual(string sha1, string sha2) =>
        Assert.That(
            string.CompareOrdinal(sha1, sha2) <= 0, Is.EqualTo(new UniqueId(sha1) <= new UniqueId(sha2)));

    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void OperatorGreaterThan(string sha1, string sha2) =>
        Assert.That(
            string.CompareOrdinal(sha1, sha2) > 0, Is.EqualTo(new UniqueId(sha1) > new UniqueId(sha2)));

    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void OperatorGreaterThanOrEqual(string sha1, string sha2) =>
        Assert.That(
            string.CompareOrdinal(sha1, sha2) >= 0, Is.EqualTo(new UniqueId(sha1) >= new UniqueId(sha2)));

    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void TryParseFailsWhenEmpty() =>
        Assert.That(
            UniqueId.TryParse(string.Empty, out var _),
            Is.False);

    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void EqualsBoxed(string sha)
    {
        var boxed = (object)new UniqueId(sha);
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
