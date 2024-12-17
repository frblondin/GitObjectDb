using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.YamlDotNet.Converters;
using NUnit.Framework;
using System;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace GitObjectDb.YamlDotNet.Tests.Converters;

public class UniqueIdConverterTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void ReadNode(UniqueId id)
    {
        // Arrange
        var sut = new UniqueIdConverter();
        using var stringReader = new StringReader($@"""{id}""");
        var parser = new Parser(stringReader);
        parser.Consume<StreamStart>();
        parser.Consume<DocumentStart>();

        // Act
        var deserialized = sut.ReadYaml(parser, typeof(UniqueId), NonImplementedObjectDeserializer);

        // Assert
        Assert.That(deserialized, Is.EqualTo(id));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void WriteNode(UniqueId id)
    {
        // Arrange
        var sut = new UniqueIdConverter();
        using var writer = new StringWriter();
        var emitter = new Emitter(writer);
        emitter.Emit(new StreamStart());
        emitter.Emit(new DocumentStart());

        // Act
        sut.WriteYaml(emitter, id, typeof(UniqueId), NonImplementedObjectSerializer);

        // Assert
        Assert.That(writer.ToString(), Is.EqualTo(id.ToString()));
    }

    private static object NonImplementedObjectDeserializer(Type type) =>
        throw new NotImplementedException();

    private static void NonImplementedObjectSerializer(object value, Type type = null) =>
        throw new NotImplementedException();
}
