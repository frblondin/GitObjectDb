using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Models
{
    public static class IMetadataObjectExtensions
    {
        readonly static JsonSerializer _jsonSerializer;

        static IMetadataObjectExtensions()
        {
            _jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            });
            _jsonSerializer.Formatting = Formatting.Indented;
        }

        public static TModel With<TModel>(this TModel source, Expression<Predicate<TModel>> predicate = null) where TModel : IMetadataObject
        {
            return (TModel)source.With(predicate);
        }

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

        internal static void ToJson(this IMetadataObject source, StringBuilder stringBuilder)
        {
            stringBuilder.Clear();
            using (var writer = new StringWriter(stringBuilder))
            {
                _jsonSerializer.Serialize(writer, source);
            }
        }
    }
}
