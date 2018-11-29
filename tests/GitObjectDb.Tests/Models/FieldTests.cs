using GitObjectDb.JsonConverters;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public class FieldTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LinkFieldSerialization(IServiceProvider serviceProvider, IObjectRepositoryContainer container, string name, string path)
        {
            // Arrange
            var link = new LazyLink<Page>(container, new ObjectPath(UniqueId.CreateNew(), path));
            var linkField = new Field.Builder(serviceProvider)
            {
                Id = UniqueId.CreateNew(),
                Name = name,
                Content = FieldContent.NewLink(new FieldLinkContent(link))
            }.ToImmutable();

            // Act
            var json = linkField.ToJObject().ToString();
            var contractResolverFactory = serviceProvider.GetRequiredService<ModelObjectContractResolverFactory>();
            var context = new ModelObjectSerializationContext(container);
            var serializer = contractResolverFactory(context).Serializer;
            using (var reader = new JsonTextReader(new StringReader(json)))
            {
                var deserialized = serializer.Deserialize<Field>(reader);
                var deserializedLazyLink = deserialized.Content.MatchOrDefault(matchLink: c => c.Target);

                // Assert
                Assert.That(deserialized.Id, Is.EqualTo(linkField.Id));
                Assert.That(deserialized.Name, Is.EqualTo(name));
                Assert.That(deserializedLazyLink.Path.Path, Is.EqualTo(path));
            }
        }
    }
}
