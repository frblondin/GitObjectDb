using GitObjectDb.Serialization.Json.Converters;
using GitObjectDb.Tools;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json
{
    internal class DefaultSerializer : INodeSerializer
    {
        private readonly ISet<Type> _approvedTypes = new HashSet<Type>();

        private readonly JsonWriterOptions _writerOptions = new JsonWriterOptions
        {
            Indented = true,
        };

        private readonly ConcurrentDictionary<string, Type> _typeBindingCache;

        public DefaultSerializer()
        {
            Options = CreateSerializerOptions();
            _typeBindingCache = new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        }

        public JsonSerializerOptions Options { get; }

        internal JsonSerializerOptions CreateSerializerOptions()
        {
            var result = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                ReferenceHandler = new NodeReferenceHandler(),
            };
            result.Converters.Add(new NonScalarConverter(this));

            return result;
        }

        public Stream Serialize(Node node)
        {
            var result = new MemoryStream();
            using var writer = new Utf8JsonWriter(result, _writerOptions);
            JsonSerializer.Serialize(writer, new NonScalar(node), Options);
            result.Seek(0L, SeekOrigin.Begin);
            return result;
        }

        public NonScalar Deserialize(Stream stream, DataPath path, string sha, Func<DataPath, ITreeItem> referenceResolver)
        {
            NodeReferenceHandler.NodeAccessor.Value = referenceResolver;
            try
            {
                var result = JsonSerializer.Deserialize<NonScalar>(stream, Options)!;
                result.Node.Path = path;

                return result;
            }
            finally
            {
                NodeReferenceHandler.NodeAccessor.Value = null;
            }
        }

        public string BindToName(Type type) => $"{type.FullName}, {type.Assembly.FullName}";

        public Type BindToType(string fullTypeName)
        {
            return _typeBindingCache.GetOrAdd(fullTypeName, ParseType);

            Type ParseType(string name)
            {
                var index = TypeHelper.GetAssemblyDelimiterIndex(name);

                var assemblyFullName = name.Substring(index + 1).Trim();
                var assemblyName = GetAssemblyName(assemblyFullName);

                // Try first to retrieve loaded assembly with no strong version check
                // ... and load assembly if none could be found
                var assembly =
                    AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
                        a => !a.IsDynamic && a.GetName().Name == assemblyName) ??
                    Assembly.Load(assemblyFullName);

                var typeName = name.Substring(0, index).Trim();
                var type = assembly.GetType(typeName);

                return type;
            }

            string GetAssemblyName(string fullName)
            {
                var index = fullName.IndexOf(',');
                return index == -1 ?
                    fullName :
                    fullName.Substring(0, index).Trim();
            }
        }
    }
}
