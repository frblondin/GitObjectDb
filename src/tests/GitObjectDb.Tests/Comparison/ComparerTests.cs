using GitObjectDb.Comparison;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System.Collections.Immutable;

namespace GitObjectDb.Tests.Comparison;

public class ComparerTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void StoreAsSeparateFilePropertyChangesGetDetected(IComparer sut, UniqueId id, string oldValue, string newValue)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder().RegisterType<SomeNode>().Build();

        // Act
        var result = sut.Compare(
            new SomeNode { Id = id, Value = oldValue },
            new SomeNode { Id = id, Value = newValue },
            model.DefaultComparisonPolicy);

        // Assert
        Assert.That(result.Differences, Has.Exactly(1).Items);
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
            .RegisterType<ObjectWithReference>()
            .RegisterType<SomeNode>()
            .Build();
        var sut = Comparer.Cache.Get(model.DefaultComparisonPolicy);
        var original = new ObjectWithReference
        {
            Reference = new SomeNode()
            {
                Value = "foo",
                Path = DataPath.Root("a", UniqueId.CreateNew(), false, "json"),
            },
        };
        var modified = original with
        {
            Reference = (SomeNode)original.Reference with { Value = "bar" },
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
            .RegisterType<ObjectWithReference>()
            .RegisterType<SomeNode>()
            .Build();
        var sut = Comparer.Cache.Get(model.DefaultComparisonPolicy);
        var original = new ObjectWithReference
        {
            Reference = new SomeNode()
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
        [StoreAsSeparateFile]
        public string Value { get; init; }
    }

    private record ObjectWithReference : Node
    {
        public Node Reference { get; init; }
    }

    private record NodeWithMultiReferences : Node
    {
        public IImmutableList<SomeNode> References { get; init; }
    }
}
