using Autofac;
using Autofac.Core;
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
        readonly IComponentContext _context;
        readonly Func<Type, string, ILazyChildren> _childResolver;

        public MetadataObjectJsonConverter(IComponentContext componentContext, Func<Type, string, ILazyChildren> childResolver)
        {
            _context = componentContext;
            _childResolver = childResolver;
        }

        public override bool CanConvert(Type objectType) => typeof(IMetadataObject).IsAssignableFrom(objectType);

        public override bool CanRead => true;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            // Load JObject from stream 
            var jObject = JObject.Load(reader);

            // Create target object based on JObject 
            var target = Create(objectType, jObject);

            // Populate the object properties 
            serializer.Populate(jObject.CreateReader(), target);

            return target;
        }

        protected object Create(Type objectType, JObject jObject)
        {
            var constructor = objectType.GetConstructors().Single(); // TODO Use constructor strategy
            var arguments = constructor.GetParameters().Select(p => ResolveParameter(p, objectType, jObject)).ToArray();
            return Activator.CreateInstance(objectType, arguments);
        }

        protected object ResolveParameter(ParameterInfo parameter, Type objectType, JObject jObject)
        {
            if (_context.ComponentRegistry.TryGetRegistration(new TypedService(parameter.ParameterType), out var registration))
            {
                return _context.ResolveComponent(registration, new Parameter[0]);
            }
            if (parameter.ParameterType.IsAssignableTo<ILazyChildren>())
            {
                return _childResolver(objectType, parameter.Name);
            }
            if (jObject.TryGetValue(parameter.Name, StringComparison.OrdinalIgnoreCase, out var token))
            {
                return token.ToObject(parameter.ParameterType);
            }
            return null;
        }

        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}
