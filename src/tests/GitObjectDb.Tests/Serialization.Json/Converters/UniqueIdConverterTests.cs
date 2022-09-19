using GitObjectDb.Model;
using GitObjectDb.Serialization;
using GitObjectDb.Serialization.Json;
using GitObjectDb.Serialization.Json.Converters;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tools;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;

namespace GitObjectDb.Tests.Serialization.Json.Converters;

public class UniqueIdConverterTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void ReadNode(UniqueId id)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder().Build();
        var serializer = new NodeSerializer(model, new Microsoft.IO.RecyclableMemoryStreamManager());

        // Act
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes($@"""{id}""").AsSpan());
        reader.Read();
        var deserialized = new UniqueIdConverter().Read(ref reader, typeof(UniqueId), serializer.Options);

        // Assert
        Assert.That(deserialized, Is.EqualTo(id));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void WriteNode(UniqueId id)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder().Build();
        var serializer = new NodeSerializer(model, new Microsoft.IO.RecyclableMemoryStreamManager());

        // Act
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            new UniqueIdConverter().Write(writer, id, serializer.Options);
        }
        stream.Position = 0;

        // Assert
        using var reader = new StreamReader(stream);
        var actual = reader.ReadToEnd();
        Assert.That(actual, Is.EqualTo($@"""{id}"""));
    }
}
