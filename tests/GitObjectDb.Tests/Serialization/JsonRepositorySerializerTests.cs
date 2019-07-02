using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using GitObjectDb.Attributes;
using GitObjectDb.Serialization.Json;
using GitObjectDb.Serialization.Json.Converters;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Utils;
using NUnit.Framework;
using GitObjectDb.Serialization;
using Microsoft.Extensions.DependencyInjection;
using GitObjectDb.Tests.Assets.Models;

namespace GitObjectDb.Tests.Serialization
{
    public partial class JsonRepositorySerializerTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ProperytWithDefaultValueGetsIgnoredFromSerialization(IServiceProvider serviceProvider, UniqueId id, string name, string value)
        {
            // Arrange
            var sut = CreateJsonRepositorySerializer(serviceProvider);
            var model = new Model(serviceProvider, id, name, value, Model.DefaultPropertyValue);

            // Act
            var json = Serialize(sut, model);

            // Assert
            Assert.That(json, Does.Not.Contain(nameof(Model.NewPropertyWithDefaultValue)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LinkFieldSerialization(IServiceProvider serviceProvider, IObjectRepositoryContainer container, string name, string path)
        {
            // Arrange
            var sut = CreateJsonRepositorySerializer(serviceProvider, new ModelObjectSerializationContext(container));
            var link = new LazyLink<Page>(container, new ObjectPath(UniqueId.CreateNew(), path));
            var linkField = new Field.Builder(serviceProvider)
            {
                Id = UniqueId.CreateNew(),
                Name = name,
                Content = FieldContent.NewLink(new FieldLinkContent(link))
            }.ToImmutable();

            // Act
            var json = Serialize(sut, linkField);
            using (var stream = new MemoryStream(Encoding.Default.GetBytes(json)))
            {
                var deserialized = (Field)sut.Deserialize(stream, _ => throw new NotSupportedException());
                var deserializedLazyLink = deserialized.Content.MatchOrDefault(matchLink: c => c.Target);

                // Assert
                Assert.That(deserialized.Id, Is.EqualTo(linkField.Id));
                Assert.That(deserialized.Name, Is.EqualTo(name));
                Assert.That(deserializedLazyLink.Path.Path, Is.EqualTo(path));
            }
        }

        private static JsonRepositorySerializer CreateJsonRepositorySerializer(IServiceProvider serviceProvider, ModelObjectSerializationContext context = null) =>
            new JsonRepositorySerializer(
                serviceProvider.GetRequiredService<ModelObjectContractCache>(),
                serviceProvider.GetRequiredService<ModelObjectSpecialValueProvider>(),
                context);

        private static string Serialize(JsonRepositorySerializer repositorySerializer, IModelObject node)
        {
            var builder = new StringBuilder();
            repositorySerializer.Serialize(node, builder);
            return builder.ToString();
        }

        [Model]
        public partial class Model
        {
            public const string DefaultPropertyValue = "Foo";

            [DataMember]
            [Modifiable]
            public string NewPropertyNoDefaultValue { get; }

            [DataMember]
            [Modifiable]
            [DefaultValue(DefaultPropertyValue)]
            public string NewPropertyWithDefaultValue { get; }
        }
    }
}
