using GitObjectDb.Serialization.Json;
using GitObjectDb.Serialization.Json.Converters;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Tests.Serialization.Json.Converters
{
    public class NonScalarConverterTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
        public void ReadNode(string pPropertyValue, string camelCasePropertyValue)
        {
            // Arrange
            var options = DefaultSerializer.CreateSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            var value = $@"
            {{
                ""{nameof(NonScalar.Type)}"": ""{NonScalarConverter.BindToName(typeof(TestNode))}"",
                ""{nameof(NonScalar.Node)}"":
                {{
                    ""{nameof(Node.Id)}"": ""{UniqueId.CreateNew()}"",
                    ""ignored"":
                    {{
                        ""nested"":
                        {{
                            ""ignoredProperty"": ""-""
                        }},
                        ""ignoredProperty"": ""-""
                    }},
                    ""P"": ""{pPropertyValue}"",
                    ""{options.PropertyNamingPolicy.ConvertName(nameof(TestNode.CamelCased))}"": ""{camelCasePropertyValue}""
                }}
            }}";

            // Act
            var reader = new Utf8JsonReader(Encoding.Default.GetBytes(value).AsSpan());
            reader.Read();
            var deserialized = new NonScalarConverter().Read(ref reader, typeof(NonScalar), options)?.Node as TestNode;

            // Assert
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Property, Is.EqualTo(pPropertyValue));
            Assert.That(deserialized.CamelCased, Is.EqualTo(camelCasePropertyValue));
        }

        public class TestNode : Node
        {
            public TestNode(UniqueId id)
                : base(id)
            {
            }

            [JsonPropertyName("P")]
            public string Property { get; set; }

            public string CamelCased { get; set; }
        }
    }
}
