using FluentValidation;
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
    public class ModelDataAccessorTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization))]
        public void ThrowIfMissingProperty(IServiceProvider serviceProvider)
        {
            // Assert
            var exception = Assert.Throws<ValidationException>(
                () => new ModelDataAccessor(serviceProvider, typeof(MissingProperty)));
            Assert.That(exception, Has.Message.Contains("could be found"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization))]
        public void ThrowIfMissingSerializableProperty(IServiceProvider serviceProvider)
        {
            // Assert
            var exception = Assert.Throws<ValidationException>(
                () => new ModelDataAccessor(serviceProvider, typeof(PropertyNotSerializable)));
            Assert.That(exception, Has.Message.Contains("is not serialized"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization))]
        public void ThrowIfDifferentPropertyType(IServiceProvider serviceProvider)
        {
            // Assert
            var exception = Assert.Throws<ValidationException>(
                () => new ModelDataAccessor(serviceProvider, typeof(PropertyTypeNotMatching)));
            Assert.That(exception, Has.Message.Contains("does not match"));
        }

        [DataContract]
        public class MissingProperty : AbstractModel
        {
            public MissingProperty(IServiceProvider serviceProvider, Guid id, string name, string someName)
                : base(serviceProvider, id, name)
            {
                NonMatchingName = someName ?? throw new ArgumentNullException(nameof(someName));
            }

            [DataMember]
            public string NonMatchingName { get; }
        }

        [DataContract]
        public class PropertyNotSerializable : AbstractModel
        {
            public PropertyNotSerializable(IServiceProvider serviceProvider, Guid id, string name, string notSerialized)
                : base(serviceProvider, id, name)
            {
                NotSerialized = notSerialized ?? throw new ArgumentNullException(nameof(notSerialized));
            }

            public string NotSerialized { get; }
        }

        [DataContract]
        public class PropertyTypeNotMatching : AbstractModel
        {
            public PropertyTypeNotMatching(IServiceProvider serviceProvider, Guid id, string name, List<string> values)
                : base(serviceProvider, id, name)
            {
                Values = values ?? throw new ArgumentNullException(nameof(values));
            }

            [DataMember]
            public IList<string> Values { get; }
        }
    }
}
