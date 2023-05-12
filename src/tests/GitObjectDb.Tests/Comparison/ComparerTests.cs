using GitObjectDb.Comparison;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;

namespace GitObjectDb.Tests.Comparison;

public class ComparerTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void EmbeddedResourceChangesGetDetected(IComparer sut, UniqueId id, string oldValue, string newValue)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder().RegisterType<SomeNode>().Build();

        // Act
        var result = sut.Compare(
            new SomeNode { Id = id, EmbeddedResource = oldValue },
            new SomeNode { Id = id, EmbeddedResource = newValue },
            model.DefaultComparisonPolicy);

        // Assert
        Assert.That(result.Differences, Has.Exactly(1).Items);
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void EmbeddedResourceUnchangedGetIgnored(IComparer sut, UniqueId id, string value)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder().RegisterType<SomeNode>().Build();

        // Act
        var result = sut.Compare(
            new SomeNode { Id = id, EmbeddedResource = value },
            new SomeNode { Id = id, EmbeddedResource = value },
            model.DefaultComparisonPolicy);

        // Assert
        Assert.That(result.Differences, Is.Empty);
    }

    [Test]
    [TestCase(null, "", true)]
    [TestCase(null, "", false)]
    public void TreatStringEmptyAndNullTheSame(string val1, string val2, bool ignoreStringLeadingTrailingWhitespace)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder().RegisterType<SomeNode>().Build();
        var comparer = Comparer.Cache.Get(
            model.DefaultComparisonPolicy with
            {
                TreatStringEmptyAndNullTheSame = ignoreStringLeadingTrailingWhitespace,
            });

        // Act
        var result = comparer.Compare(val1, val2);

        // Assert
        Assert.That(result.AreEqual, Is.EqualTo(ignoreStringLeadingTrailingWhitespace));
    }

    [Test]
    [TestCase(" \nfoo\n ", "foo", true)]
    [TestCase(" \nfoo\n ", "foo", false)]
    public void IgnoreStringLeadingTrailingWhitespace(string val1, string val2, bool ignoreStringLeadingTrailingWhitespace)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder().RegisterType<SomeNode>().Build();
        var comparer = Comparer.Cache.Get(
            model.DefaultComparisonPolicy with
            {
                IgnoreStringLeadingTrailingWhitespace = ignoreStringLeadingTrailingWhitespace,
            });

        // Act
        var result = comparer.Compare(val1, val2);

        // Assert
        Assert.That(result.AreEqual, Is.EqualTo(ignoreStringLeadingTrailingWhitespace));
    }

    private record SomeNode : Node
    {
#pragma warning disable S1144 // Unused private types or members should be removed
        public string Value { get; set; }
#pragma warning restore S1144 // Unused private types or members should be removed
    }
}
