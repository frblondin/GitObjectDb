using GitObjectDb.Model;
using GitObjectDb.SystemTextJson.Converters;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace GitObjectDb.SystemTextJson.Tests.Converters;

public class UniqueIdConverterTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void ReadNode(UniqueId id)
    {
        // Arrange
        var model = new ConventionBaseModelBuilder().Build();
        var serializer = new NodeSerializer(model, Options.Create(new JsonSerializerOptions()));

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
        var serializer = new NodeSerializer(model);

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
