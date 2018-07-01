using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// A set of methods for instances of <see cref="IMetadataObject"/>.
    /// </summary>
    public static class IMetadataObjectExtensions
    {
        static readonly JsonSerializer _jsonSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            Formatting = Formatting.Indented
        });

        /// <summary>
        /// Creates a copy of the instance and apply changes according to the new test values provided in the predicate.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The newly created copy. Both parents and children nodes have been cloned as well.</returns>
        public static TModel With<TModel>(this TModel source, Expression<Predicate<TModel>> predicate = null)
            where TModel : IMetadataObject
        {
            return (TModel)source.With(predicate);
        }

        /// <summary>
        /// Gets the root instance of the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The root <see cref="IMetadataObject"/> instance.</returns>
        /// <exception cref="ArgumentNullException">node</exception>
        internal static IMetadataObject Root(this IMetadataObject node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            while (node.Parent != null)
            {
                node = node.Parent;
            }

            return node;
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{IMetadataObject}"/> containing all parents of this node.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>All parent nodes from nearest to farest.</returns>
        public static IEnumerable<IMetadataObject> Parents(this IMetadataObject source)
        {
            var node = source;
            while (node != null)
            {
                yield return node;
                node = node.Parent;
            }
        }

        /// <summary>
        /// Flattens the specified source and its nested children.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>An enumerable containing the source and its nested children.</returns>
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

        /// <summary>
        /// Serializes the node into the <paramref name="stringBuilder"/>.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="stringBuilder">The string builder.</param>
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
