using GitObjectDb.Model;
using GitObjectDb.SystemTextJson.Converters;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.YamlDotNet.Converters;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.NamingConventions;

namespace GitObjectDb.YamlDotNet.Tests.Converters;

public class UniqueIdConverterTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void ReadNode(UniqueId id)
    {
        // Arrange
        using var stringReader = new StringReader($@"""{id}""");
        var parser = new Parser(stringReader);
        parser.Consume<StreamStart>();
        parser.Consume<DocumentStart>();

        // Act
        var deserialized = new UniqueIdConverter().ReadYaml(parser, typeof(UniqueId));

        // Assert
        Assert.That(deserialized, Is.EqualTo(id));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void WriteNode(UniqueId id)
    {
        // Arrange
        using var writer = new StringWriter();
        var emitter = new Emitter(writer);
        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart());

        // Act
        new UniqueIdConverter().WriteYaml(emitter, id, typeof(UniqueId));

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(id.ToString()));
    }
}
