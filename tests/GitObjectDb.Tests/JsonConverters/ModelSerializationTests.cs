using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using GitObjectDb.Attributes;
using GitObjectDb.JsonConverters;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace GitObjectDb.Tests.JsonConverters
{
    public partial class ModelSerializationTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ProperytWithDefaultValueGetsIgnoredFromSerialization(IServiceProvider serviceProvider, IObjectRepositoryContainer<ObjectRepository> container, UniqueId id, string name, string value)
        {
            // Arrange
            var contractResolverFactory = serviceProvider.GetRequiredService<ModelObjectContractResolverFactory>();
            var serializer = contractResolverFactory(new ModelObjectSerializationContext(container)).Serializer;
            var model = new Model(serviceProvider, id, name, value, Model.DefaultPropertyValue);

            // Act
            var json = model.ToJObject();

            // Assert
            Assert.That(json.Properties().Where(p => p.Name.Equals(nameof(Model.NewPropertyWithDefaultValue), StringComparison.OrdinalIgnoreCase)), Is.Empty);
        }

        private static string Serialize(Model model)
        {
            using (var writer = new StringWriter())
            {
                JsonSerializerProvider.Default.Serialize(writer, model);
                return writer.ToString();
            }
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
