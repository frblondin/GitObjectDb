using GitObjectDb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.JsonConverters
{
    /// <summary>
    /// Converts <see cref="IMetadataObject"/> objects.
    /// </summary>
    /// <seealso cref="JsonConverter" />
    internal class MetadataObjectJsonConverter : JsonConverter
    {
        readonly IServiceProvider _serviceProvider;
        readonly ChildrenResolver _childResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataObjectJsonConverter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="childResolver">The child resolver.</param>
        public MetadataObjectJsonConverter(IServiceProvider serviceProvider, ChildrenResolver childResolver)
        {
            _serviceProvider = serviceProvider;
            _childResolver = childResolver;
        }

        /// <summary>
        /// Resolves children from the property name.
        /// </summary>
        /// <param name="parentType">Type of the parent.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>An <see cref="ILazyChildren"/> instance containing the children.</returns>
        internal delegate ILazyChildren ChildrenResolver(Type parentType, string propertyName);

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override bool CanRead => true;

        static object ResolveFromJsonToken(JsonProperty property, JObject jObject) =>
            jObject.TryGetValue(property.PropertyName, StringComparison.OrdinalIgnoreCase, out var token) ?
            token.ToObject(property.PropertyType) :
            null;

        /// <inheritdoc />
        public override bool CanConvert(Type objectType) =>
            typeof(IMetadataObject).IsAssignableFrom(objectType);

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jObject = JObject.Load(reader);
            var contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(objectType);
            var result = Create(objectType, jObject, contract);

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), result);

            return result;
        }

        object Create(Type objectType, JObject jObject, JsonObjectContract contract)
        {
            var arguments = contract.CreatorParameters.Select(p =>
                ResolveParameter(p, objectType, jObject)).ToArray();
            return Activator.CreateInstance(objectType, arguments);
        }

        object ResolveParameter(JsonProperty property, Type objectType, JObject jObject) =>
            ResolveChildren(property, objectType) ??
            ResolveFromJsonToken(property, jObject) ??
            ResolveFromServiceProvider(property) ??
            throw new NotImplementedException($"Unable to create parameter '{property.PropertyName}' of type '{property.PropertyType}'.");

        object ResolveChildren(JsonProperty property, Type objectType) =>
            typeof(ILazyChildren).IsAssignableFrom(property.PropertyType) ?
            _childResolver(objectType, property.PropertyName) :
            null;

        object ResolveFromServiceProvider(JsonProperty property) =>
            _serviceProvider.GetService(property.PropertyType);

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            throw new NotImplementedException();
    }
}
