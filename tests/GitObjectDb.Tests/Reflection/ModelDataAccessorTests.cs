using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Utils;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Tests.Reflection
{
    public partial class ModelDataAccessorTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization))]
        public void ThrowIfMissingSerializableProperty(IServiceProvider serviceProvider)
        {
            // Assert
            var exception = Assert.Throws<NotSupportedException>(
                () => new ModelDataAccessor(serviceProvider, typeof(PropertyNotSerializable)));
            Assert.That(exception, Has.Message.Contains("is not serialized"));
        }

        [Model]
        public partial class PropertyNotSerializable
        {
            public string NotSerialized { get; }
        }
    }
}
