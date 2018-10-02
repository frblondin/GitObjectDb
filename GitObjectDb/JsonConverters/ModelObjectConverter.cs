using GitObjectDb.Attributes;
using GitObjectDb.Models;
using GitObjectDb.Services;
using Microsoft.Extensions.DependencyInjection;
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
    /// Converts <see cref="IModelObject"/> objects.
    /// </summary>
    /// <seealso cref="JsonConverter" />
    internal class ModelObjectConverter : JsonConverter
    {
        readonly IEnumerable<ICreatorParameterResolver> _creatorParameterResolvers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelObjectConverter"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="childResolver">The child resolver.</param>
        /// <param name="repositoryContainer">The repository container.</param>
        public ModelObjectConverter(IServiceProvider serviceProvider, ChildrenResolver childResolver, IObjectRepositoryContainer repositoryContainer)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            ChildResolver = childResolver ?? throw new ArgumentNullException(nameof(childResolver));
            RepositoryContainer = repositoryContainer ?? throw new ArgumentNullException(nameof(repositoryContainer));
            _creatorParameterResolvers = serviceProvider.GetRequiredService<IEnumerable<ICreatorParameterResolver>>();
        }

        /// <summary>
        /// Gets the repository container.
        /// </summary>
        internal IObjectRepositoryContainer RepositoryContainer { get; }

        /// <summary>
        /// Gets the child resolver.
        /// </summary>
        internal ChildrenResolver ChildResolver { get; }

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override bool CanRead => true;

        static object ResolveFromJsonToken(JsonProperty property, JObject jObject)
        {
            if (jObject.TryGetValue(property.PropertyName, StringComparison.OrdinalIgnoreCase, out var token))
            {
                var typeName = !(token is JArray) ?
                    (token as JContainer)?.Value<string>("$type") :
                    null;
                var type = !string.IsNullOrEmpty(typeName) ?
                    Type.GetType(typeName) :
                    property.PropertyType;
                return token.ToObject(type);
            }
            return null;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }

            return typeof(IModelObject).IsAssignableFrom(objectType);
        }

        /// <inheritdoc />
        [ExcludeFromGuardForNull]
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (objectType == null)
            {
                throw new ArgumentNullException(nameof(objectType));
            }
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

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

        object ResolveParameter(JsonProperty property, Type objectType, JObject jObject)
        {
            var instance = ResolveFromJsonToken(property, jObject);
            if (instance != null)
            {
                return instance;
            }
            var resolver = _creatorParameterResolvers.FirstOrDefault(r => r.CanResolve(property, objectType));
            return resolver?.Resolve(this, property, objectType) ??
                throw new NotImplementedException($"Unable to create parameter '{property.PropertyName}' of type '{property.PropertyType}'.");
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            throw new NotImplementedException();
        }
    }
}
