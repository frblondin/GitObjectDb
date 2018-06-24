using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace GitObjectDb.Models
{
    public static class IMetadataObjectExtensions
    {
        internal static IMetadataObject Root(this IMetadataObject node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            while (node.Parent != null) node = node.Parent;
            return node;
        }

        public static IEnumerable<IMetadataObject> Parents(this IMetadataObject source)
        {
            var node = source;
            while (node != null)
            {
                yield return node;
                node = node.Parent;
            }
        }

        internal static IEnumerable<IMetadataObject> Flatten(this IMetadataObject source)
        {
            yield return source;
            foreach (var child in source.Children)
            {
                foreach (var flattened in child.Flatten())
                {
                    yield return flattened;
                }
            }
        }

        internal static string ToJson(this IMetadataObject source) =>
            JsonConvert.SerializeObject(source, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
    }
}
