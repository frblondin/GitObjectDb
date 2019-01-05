using GitObjectDb.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// A set of methods for instances of <see cref="IModelObject"/>.
    /// </summary>
    public static class IModelObjectExtensions
    {
        /// <summary>
        /// Gets the root repository of the specified node.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The root <see cref="IModelObject"/> instance.</returns>
        /// <exception cref="ArgumentNullException">node</exception>
        public static IModelObject Root(this IModelObject node)
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
        public static bool IsParentOf(this IModelObject source, IModelObject instance)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var node = instance;
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
        /// Gets an <see cref="IEnumerable{IModelObject}"/> containing all parents of this node including itself.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>All parent nodes from nearest to farest.</returns>
        public static IEnumerable<IModelObject> Parents(this IModelObject source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return ParentsIterator(source);
        }

        private static IEnumerable<IModelObject> ParentsIterator(IModelObject source)
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
        internal static IEnumerable<IModelObject> Flatten(this IModelObject source)
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
        internal static string GetFolderPath(this IModelObject source)
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
        internal static string GetDataPath(this IModelObject source)
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

        private static void GetFolderPath(IModelObject node, StringBuilder builder)
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
        internal static void ToJson(this IModelObject source, StringBuilder stringBuilder)
        {
            stringBuilder.Clear();
            using (var writer = new StringWriter(stringBuilder))
            {
                JsonSerializerProvider.Default.Serialize(writer, source);
            }
        }

        /// <summary>
        /// Creates a <see cref="JObject"/> from a node.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>A <see cref="JObject"/> with the values of the specified node.</returns>
        internal static JObject ToJObject(this IModelObject source) =>
            JObject.FromObject(source, JsonSerializerProvider.Default);
    }
}
