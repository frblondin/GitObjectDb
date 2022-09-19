using AutoFixture;
using GitObjectDb.Model;
using GitObjectDb.Serialization;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System;

namespace GitObjectDb.Tests.Serialization.Json;

public partial class NodeSerializerTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void EmbeddedResourceGetPreserved(IFixture fixture)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder()
            .RegisterType<SomeNode>()
            .Build();

        // Arrange
        var value = new SomeNode
        {
            EmbeddedResource = "\nSome\nValueContaining Special chars such as /*, */, or //.\n",
            Path = new DataPath("Nodes", "foo.json", false),
        };

        // Act
        var nodeSerializer = fixture.Create<INodeSerializer>();
        var serialized = nodeSerializer.Serialize(value);
        var deserialized = nodeSerializer.Deserialize(serialized, null, model, _ => throw new NotImplementedException());

        // Assert
        Assert.That(deserialized.EmbeddedResource, Is.EqualTo(value.EmbeddedResource));
    }

    private record SomeNode : Node
    {
    }
}
