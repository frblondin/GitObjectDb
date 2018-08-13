using GitObjectDb.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// Creates a copy of the repository and apply changes according to the new test values provided in the predicate.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The newly created copy. Both parents and children nodes have been cloned as well.</returns>
        public static TModel With<TModel>(this TModel source, Expression<Predicate<TModel>> predicate = null)
            where TModel : IMetadataObject
        {
            return With(source, new PredicateReflector(source, predicate));
        }

        /// <summary>
        /// Creates a copy of the repository and apply changes according to the new test values provided in the predicate.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The newly created copy. Both parents and children nodes have been cloned as well.</returns>
        public static TModel With<TModel>(this TModel source, IPredicateReflector predicate)
            where TModel : IMetadataObject
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return (TModel)source.DataAccessor.With(source, predicate);
        }

        /// <summary>
        /// Gets the root repository of the specified node.
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
        /// Determines whether this node is a parent of the specified instance.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>
        ///   <c>true</c> if the node is a parent of the specified instance; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsParentOf(this IMetadataObject source, IMetadataObject instance)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var node = instance.Parent;
            while (node != null)
            {
                if (node == source)
                {
                    return true;
                }
                node = node.Parent;
            }
            return false;
        }

        /// <summary>
        /// Gets an <see cref="IEnumerable{IMetadataObject}"/> containing all parents of this node.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>All parent nodes from nearest to farest.</returns>
        public static IEnumerable<IMetadataObject> Parents(this IMetadataObject source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ParentsIterator(source);
        }

        static IEnumerable<IMetadataObject> ParentsIterator(IMetadataObject source)
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
        /// Gets the folder path containing the data file for a node.
        /// </summary>
        /// <param name="source">The node.</param>
        /// <returns>A <see cref="string"/> value containing the path to the folder.</returns>
        internal static string GetFolderPath(this IMetadataObject source)
        {
            var result = new StringBuilder();
            GetFolderPath(source, result);
            return result.ToString();
        }

        /// <summary>
        /// Gets the path to the data file for a node.
        /// </summary>
        /// <param name="source">The node.</param>
        /// <returns>A <see cref="string"/> value containing the path to the data file.</returns>
        internal static string GetDataPath(this IMetadataObject source)
        {
            var result = new StringBuilder();
            GetFolderPath(source, result);
            if (result.Length > 0)
            {
                result.Append('/');
            }
            result.Append(FileSystemStorage.DataFile);
            return result.ToString();
        }

        static void GetFolderPath(IMetadataObject node, StringBuilder builder)
        {
            if (node.Parent != null)
            {
                GetFolderPath(node.Parent, builder);
                var childProperty = node.Parent.DataAccessor.ChildProperties.Single(p => p.ItemType.IsInstanceOfType(node));
                if (builder.Length > 0)
                {
                    builder.Append('/');
                }
                builder.Append(childProperty.Name);
                builder.Append('/');
            }
            if (!(node is IObjectRepository))
            {
                builder.Append(node.Id);
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
