using GitObjectDb.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Utils
{
    public class MetadataObjectJsonConverter : JsonConverter
    {
        readonly IServiceProvider _serviceProvider;
        readonly Func<Type, string, ILazyChildren> _childResolver;

        public MetadataObjectJsonConverter(IServiceProvider serviceProvider, Func<Type, string, ILazyChildren> childResolver)
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
            var result = Create(objectType, jObject);

            // Populate the object properties 
            serializer.Populate(jObject.CreateReader(), result);

            return result;
        }

        protected object Create(Type objectType, JObject jObject)
        {
            var constructor = objectType.GetConstructors().Single(); // TODO Use constructor strategy
            var arguments = constructor.GetParameters().Select(p => ResolveParameter(p, objectType, jObject)).ToArray();
            return Activator.CreateInstance(objectType, arguments);
        }

        protected object ResolveParameter(ParameterInfo parameter, Type objectType, JObject jObject) =>
            ResolveChildren(parameter, objectType) ??
            ResolveFromJsonToken(parameter, jObject) ??
            ResolveFromServiceProvider(parameter);

        object ResolveChildren(ParameterInfo parameter, Type objectType) =>
            typeof(ILazyChildren).IsAssignableFrom(parameter.ParameterType) ?
            _childResolver(objectType, parameter.Name) :
            null;

        static object ResolveFromJsonToken(ParameterInfo parameter, JObject jObject) =>
            jObject.TryGetValue(parameter.Name, StringComparison.OrdinalIgnoreCase, out var token) ?
            token.ToObject(parameter.ParameterType) :
            null;

        object ResolveFromServiceProvider(ParameterInfo parameter) =>
            _serviceProvider.GetService(parameter.ParameterType);

        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
            throw new NotImplementedException();
    }
}
