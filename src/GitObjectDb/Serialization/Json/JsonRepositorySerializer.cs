using GitObjectDb.Models;
using GitObjectDb.Serialization.Json.Converters;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Serialization.Json
{
    internal class JsonRepositorySerializer : IObjectRepositorySerializer
    {
        private readonly JsonSerializer _serializer;
        private readonly ModelObjectSpecialValueProvider _specialValueProvider;

        [ActivatorUtilitiesConstructor]
        public JsonRepositorySerializer(ModelObjectContractCache modelObjectContractCache, ModelObjectSpecialValueProvider specialValueProvider,
            ModelObjectSerializationContext context = null)
        {
            _specialValueProvider = specialValueProvider ?? throw new ArgumentNullException(nameof(specialValueProvider));
            _serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = CreateContractResolver(modelObjectContractCache, context),
                TypeNameHandling = TypeNameHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
            };
            _serializer.Converters.Add(new VersionConverter());
        }

        private static CamelCasePropertyNamesContractResolver CreateContractResolver(ModelObjectContractCache modelObjectContractCache, ModelObjectSerializationContext context) =>
            context != null ? new ModelObjectContractResolver(context, modelObjectContractCache) : new CamelCasePropertyNamesContractResolver();

        public IModelObject Deserialize(Stream stream, Func<string, string> relativeFileDataResolver)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }
            if (relativeFileDataResolver is null)
            {
                throw new ArgumentNullException(nameof(relativeFileDataResolver));
            }

            using (var streamReader = new StreamReader(stream))
            {
                return (IModelObject)_serializer.Deserialize(new JsonModelObjectReader(relativeFileDataResolver, streamReader));
            }
        }

        public IList<ModelNestedObjectInfo> Serialize(IModelObject node, StringBuilder builder)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            using (var writer = new StringWriter(builder))
            {
                var jsonWriter = new JsonModelObjectWriter(node, writer);
                _serializer.Serialize(jsonWriter, node);
                return jsonWriter.AdditionalObjects;
            }
        }

        public void ValidateSerializable(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var contract = (JsonObjectContract)_serializer.ContractResolver.ResolveContract(type);
            var missingMatchingProperties = contract.CreatorParameters.SelectMany(GetParameterErrors).ToList();
            if (missingMatchingProperties.Any())
            {
                throw new NotSupportedException(
                    $"The type {type.Name} contains invalid constructor parameters:\n\t" +
                    string.Join("\n\t", missingMatchingProperties));
            }

            IEnumerable<string> GetParameterErrors(JsonProperty constructorParameter)
            {
                if (_specialValueProvider.TryGetInjector(type, constructorParameter) != null)
                {
                    yield break;
                }
                var matching = contract.Properties.TryGetWithValue(p => p.PropertyName, constructorParameter.PropertyName);
                if (matching == null)
                {
                    yield return $"No property named '{constructorParameter.PropertyName}' could be found.";
                }
                if (matching.Ignored)
                {
                    yield return $"The property named '{constructorParameter.PropertyName}' is not serialized.";
                }
                if (matching.PropertyType != constructorParameter.PropertyType)
                {
                    yield return $"The property type '{matching.PropertyType}' does not match.";
                }
            }
        }
    }
}
