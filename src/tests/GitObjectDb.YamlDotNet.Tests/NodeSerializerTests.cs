using AutoFixture;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;

namespace GitObjectDb.YamlDotNet.Tests;

public partial class NodeSerializerTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization))]
    public void EmbeddedResourceGetPreserved(IFixture fixture)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder()
            .RegisterType<SomeNode>()
            .Build();
        fixture.Do<IServiceCollection>(services => services.AddSingleton(model));

        // Arrange
        var value = new SomeNode
        {
            EmbeddedResource = "\nSome\nValue containing Special chars such as #, /*, */, or //.\n",
            Path = new DataPath("Nodes", "foo.yaml", false),
        };

        // Act
        var nodeSerializer = fixture.Create<INodeSerializer>();
        var serialized = nodeSerializer.Serialize(value);
        var deserialized = nodeSerializer.Deserialize(serialized, ObjectId.Zero, null, _ => throw new NotImplementedException());

        // Assert
        Assert.That(deserialized.EmbeddedResource, Is.EqualTo(value.EmbeddedResource));
    }

    private record SomeNode : Node
    {
    }
}
