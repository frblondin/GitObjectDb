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
    internal delegate ILazyChildren ChildrenResolver(Type parentType, string propertyName);

    internal class MetadataObjectJsonConverter : JsonConverter
    {
        readonly IServiceProvider _serviceProvider;
        readonly ChildrenResolver _childResolver;

        public MetadataObjectJsonConverter(IServiceProvider serviceProvider, ChildrenResolver childResolver)
        {
            _serviceProvider = serviceProvider;
            _childResolver = childResolver;
        }

        public override bool CanConvert(Type objectType) =>
            typeof(IMetadataObject).IsAssignableFrom(objectType);

        public override bool CanRead => true;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var jObject = JObject.Load(reader);
            var contract = (JsonObjectContract)serializer.ContractResolver.ResolveContract(objectType);
            var result = Create(objectType, jObject, contract);

            // Populate the object properties 
            serializer.Populate(jObject.CreateReader(), result);

            return result;
        }

        protected object Create(Type objectType, JObject jObject, JsonObjectContract contract)
        {
            var arguments = contract.CreatorParameters.Select(p =>
                ResolveParameter(p, objectType, jObject)).ToArray();
            return Activator.CreateInstance(objectType, arguments);
        }

        protected object ResolveParameter(JsonProperty property, Type objectType, JObject jObject) =>
            ResolveChildren(property, objectType) ??
            ResolveFromJsonToken(property, jObject) ??
            ResolveFromServiceProvider(property) ??
            throw new NotImplementedException($"Unable to create parameter '{property.PropertyName}' of type '{property.PropertyType}'.");

        object ResolveChildren(JsonProperty property, Type objectType) =>
            typeof(ILazyChildren).IsAssignableFrom(property.PropertyType) ?
            _childResolver(objectType, property.PropertyName) :
            null;

        static object ResolveFromJsonToken(JsonProperty property, JObject jObject) =>
            jObject.TryGetValue(property.PropertyName, StringComparison.OrdinalIgnoreCase, out var token) ?
            token.ToObject(property.PropertyType) :
            null;

        object ResolveFromServiceProvider(JsonProperty property) =>
            _serviceProvider.GetService(property.PropertyType);

        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            throw new NotImplementedException();
    }
}
