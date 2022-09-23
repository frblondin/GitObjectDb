using GitObjectDb.Model;
using NUnit.Framework;
using System;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace GitObjectDb.SystemTextJson.Tests;

public class NodeTypeInfoResolverTests
{
    [Test]
    public void UsesStrictDerivedTypeHandling()
    {
        // Arrange
        var model = new ConventionBaseModelBuilder()
            .RegisterType<Node>()
            .Build();

        // Act
        var sut = new NodeTypeInfoResolver(model);
        var info = sut.GetTypeInfo(typeof(Node), new());

        // Assert
        Assert.That(
            info,
            Has.Property(nameof(JsonTypeInfo.PolymorphismOptions))
               .Property(nameof(JsonPolymorphismOptions.UnknownDerivedTypeHandling))
               .EqualTo(JsonUnknownDerivedTypeHandling.FailSerialization));
    }

    [Test]
    public void DoesNotAddAbstractTypes()
    {
        // Arrange
        var model = new ConventionBaseModelBuilder()
            .RegisterType<AbstractNode>()
            .RegisterType<ConcreteNode>()
            .Build();

        // Act
        var sut = new NodeTypeInfoResolver(model);
        var info = sut.GetTypeInfo(typeof(Node), new());

        // Assert
        Assert.That(
            info.PolymorphismOptions.DerivedTypes,
            Has.Exactly(1).Items
               .With.Property(nameof(JsonDerivedType.DerivedType))
                    .EqualTo(typeof(ConcreteNode)));
        Assert.That(
            info.PolymorphismOptions.DerivedTypes,
            Has.Exactly(0).Items
               .With.Property(nameof(JsonDerivedType.DerivedType))
                    .EqualTo(typeof(AbstractNode)));
    }

    public abstract record AbstractNode : Node
    {
    }

    public record ConcreteNode : AbstractNode
    {
    }
}
