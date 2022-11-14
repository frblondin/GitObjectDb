using AutoFixture;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Reflection;

namespace GitObjectDb.YamlDotNet.Tests;

public partial class NodeSerializerTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void DeserializerUpdatesToUpperVersion(IFixture fixture)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder()
            .RegisterType<SomeNodeV1>()
            .RegisterType<SomeNodeV2>()
            .AddDeprecatedNodeUpdater(UpdateDeprecatedNode)
            .Build();

        // Act
        var nodeSerializer = fixture.Create<INodeSerializer.Factory>().Invoke(model);
        var node = new SomeNodeV1
        {
            Id = new UniqueId("id"),
            Path = new DataPath("Items", "id.json", false),
            Flags = (int)(BindingFlags.Public | BindingFlags.Instance),
        };
        var serialized = nodeSerializer.Serialize(node);
        var deserialized = (SomeNodeV2)nodeSerializer.Deserialize(serialized, ObjectId.Zero, node.Path, _ => throw new NotImplementedException());

        // Assert
        Assert.That(deserialized.TypedFlags, Is.EqualTo(BindingFlags.Public | BindingFlags.Instance));
    }

    private Node UpdateDeprecatedNode(Node old, Type targetType)
    {
        var nodeV1 = (SomeNodeV1)old;
        return new SomeNodeV2
        {
            Id = old.Id,
            TypedFlags = (BindingFlags)nodeV1.Flags,
        };
    }

    [GitFolder(FolderName = "Items", UseNodeFolders = false)]
    [IsDeprecatedNodeType(typeof(SomeNodeV2))]
    private record SomeNodeV1 : Node
    {
        public int Flags { get; set; }
    }

    [GitFolder(FolderName = "Items", UseNodeFolders = false)]
    private record SomeNodeV2 : Node
    {
        public BindingFlags TypedFlags { get; set; }
    }
}
