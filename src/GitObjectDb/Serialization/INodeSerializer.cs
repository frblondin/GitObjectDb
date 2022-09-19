using GitObjectDb.Model;
using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.IO;
using System.Text.Json;

namespace GitObjectDb.Serialization;

internal delegate INodeSerializer NodeSerializerFactory(IDataModel model);

/// <summary>Provides methods to serialize and deserialize nodes.</summary>
public interface INodeSerializer
{
    /// <summary>
    /// Parses the UTF-8 encoded stream representing a Node value into an object.
    /// </summary>
    /// <param name="stream">Stream containing the JSON text to parse.</param>
    /// <param name="treeId">The id of the tree where the stream is originated from.</param>
    /// <param name="path">Path of the Node.</param>
    /// <param name="referenceResolver">The delegate that returns referenced nodes.</param>
    /// <returns>A <see cref="Node"/> representation of the JSON value.</returns>
    Node Deserialize(Stream stream,
                     ObjectId treeId,
                     DataPath path,
                     ItemLoader referenceResolver);

    /// <summary>
    /// Transforms the <paramref name="node"/> into a JSON representation.
    /// </summary>
    /// <param name="node">The node to be converted.</param>
    /// <returns>The stream containing the JSON representation.</returns>
    Stream Serialize(Node node);

    /// <summary>
    /// Creates a RegEx representation of a value matching provided pattern.
    /// </summary>
    /// <param name="pattern">The pattern to be escaped by serializer.</param>
    /// <returns>Adapted RegEx pattern.</returns>
    string EscapeRegExPattern(string pattern);
}
