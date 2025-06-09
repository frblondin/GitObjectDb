using AutoFixture;
using GitDotNet;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace GitObjectDb.SystemTextJson.Tests;

public partial class NodeSerializerTests
{
    [Test]
    public async Task SimpleValueGetPreserved()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultServiceProviderCustomization());
        var model = new ConventionBaseModelBuilder()
            .RegisterType<SomeNode>()
            .Build();
        fixture.Do<IServiceCollection>(services => services.AddSingleton(model));

        // Arrange
        var value = new SomeNode
        {
            Value = "\nSome\nValueContaining Special chars such as /*, */, or //.\n",
            Path = new DataPath("Nodes", "foo.json", false),
        };

        // Act
        var nodeSerializer = fixture.Create<INodeSerializer>();
        var serialized = nodeSerializer.Serialize(value);
        var deserialized = (SomeNode)await nodeSerializer.DeserializeAsync(serialized, HashId.Empty, null, _ => throw new NotImplementedException());

        // Assert
        Assert.That(deserialized.Value, Is.EqualTo(value.Value));
    }

    private record SomeNode : Node
    {
        required public string Value { get; init; }
    }
}
