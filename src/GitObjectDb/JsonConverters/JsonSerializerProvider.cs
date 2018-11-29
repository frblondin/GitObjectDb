using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.JsonConverters
{
    /// <summary>
    /// <see cref="JsonSerializer"/> provider.
    /// </summary>
    internal static class JsonSerializerProvider
    {
        /// <summary>
        /// Gets the default serializer.
        /// </summary>
        internal static JsonSerializer Default { get; } = Create();

        /// <summary>
        /// Creates a new serializer using the specified contract resolver.
        /// </summary>
        /// <param name="contractResolver">The contract resolver.</param>
        /// <returns>The <see cref="JsonSerializer"/>.</returns>
        internal static JsonSerializer Create(IContractResolver contractResolver = null)
        {
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = contractResolver ?? new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.Indented
            };
            serializer.Converters.Add(new VersionConverter());
            return serializer;
        }
    }
}
