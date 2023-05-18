using System.Collections.Immutable;
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

    [Test]
    public void IgnoreReferencedNodeChange()
    {
        // Arrange
        var model = new ConventionBaseModelBuilder()
            .RegisterType<NodeWithReference>()
            .RegisterType<SomeNode>()
            .Build();
        var sut = Comparer.Cache.Get(model.DefaultComparisonPolicy);
        var original = new NodeWithReference
        {
            Reference = new()
            {
                Value = "foo",
                Path = DataPath.Root("a", UniqueId.CreateNew(), false, "json"),
            },
        };
        var modified = original with
        {
            Reference = original.Reference with { Value = "bar" },
        };

        // Act
        var result = sut.Compare(original, modified);

        // Assert
        Assert.That(result.AreEqual, Is.True);
    }

    [Test]
    public void DetectReferencedPathChange()
    {
        // Arrange
        var model = new ConventionBaseModelBuilder()
            .RegisterType<NodeWithReference>()
            .RegisterType<SomeNode>()
            .Build();
        var sut = Comparer.Cache.Get(model.DefaultComparisonPolicy);
        var original = new NodeWithReference
        {
            Reference = new()
            {
                Value = "foo",
                Path = DataPath.Root("a", UniqueId.CreateNew(), false, "json"),
            },
        };
        var modified = original with
        {
            Reference = original.Reference with
            {
                Path = DataPath.Root("a", UniqueId.CreateNew(), false, "json"),
            },
        };

        // Act
        var result = sut.Compare(original, modified);

        // Assert
        Assert.That(result.AreEqual, Is.False);
    }

    [Test]
    public void IgnoreMultiReferencedNodeChange()
    {
        // Arrange
        var model = new ConventionBaseModelBuilder()
            .RegisterType<NodeWithMultiReferences>()
            .RegisterType<SomeNode>()
            .Build();
        var sut = Comparer.Cache.Get(model.DefaultComparisonPolicy);
        var original = new NodeWithMultiReferences
        {
            References = ImmutableList.Create(new SomeNode()
            {
                Value = "foo",
                Path = DataPath.Root("a", UniqueId.CreateNew(), false, "json"),
            }),
        };
        var modified = original with
        {
            References = ImmutableList.Create(
                original.References[0] with { Value = "bar" }),
        };

        // Act
        var result = sut.Compare(original, modified);

        // Assert
        Assert.That(result.AreEqual, Is.True);
    }

    [Test]
    public void DetectMultiReferencedPathChange()
    {
        // Arrange
        var model = new ConventionBaseModelBuilder()
            .RegisterType<NodeWithMultiReferences>()
            .RegisterType<SomeNode>()
            .Build();
        var sut = Comparer.Cache.Get(model.DefaultComparisonPolicy);
        var original = new NodeWithMultiReferences
        {
            References = ImmutableList.Create(new SomeNode()
            {
                Value = "foo",
                Path = DataPath.Root("a", UniqueId.CreateNew(), false, "json"),
            }),
        };
        var modified = original with
        {
            References = ImmutableList.Create(original.References[0] with
            {
                Path = DataPath.Root("a", UniqueId.CreateNew(), false, "json"),
            }),
        };

        // Act
        var result = sut.Compare(original, modified);

        // Assert
        Assert.That(result.AreEqual, Is.False);
    }

    private record SomeNode : Node
    {
        public string Value { get; init; }
    }

    private record NodeWithReference : Node
    {
        public SomeNode Reference { get; init; }
    }

    private record NodeWithMultiReferences : Node
    {
        public IImmutableList<SomeNode> References { get; init; }
    }
}
