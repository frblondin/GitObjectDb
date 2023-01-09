using GitObjectDb.Model;
using LibGit2Sharp;
using System.IO;

namespace GitObjectDb;

/// <summary>Provides methods to serialize and deserialize nodes.</summary>
public interface INodeSerializer
{
    /// <summary>Provides a node serializer.</summary>
    /// <param name="model">The model of GitObjectDb.</param>
    /// <returns>The serializer to be used by GitObjectDb.</returns>
    public delegate INodeSerializer Factory(IDataModel model);

    /// <summary>Represents a method that creates a <see cref="TreeItem"/> from a path.</summary>
    /// <param name="path">The path of item.</param>
    /// <returns>An item.</returns>
    public delegate TreeItem ItemLoader(DataPath path);

    /// <summary>Gets the extension of serialized files (json, yaml...).</summary>
    string FileExtension { get; }

    /// <summary>
    /// Parses the UTF-8 encoded stream representing a Node value into an object.
    /// </summary>
    /// <param name="stream">Stream containing the text to parse.</param>
    /// <param name="treeId">The id of the tree where the stream is originated from.</param>
    /// <param name="path">Path of the Node.</param>
    /// <param name="referenceResolver">The delegate that returns referenced nodes.</param>
    /// <returns>A <see cref="Node"/> representation of the text value.</returns>
    Node Deserialize(Stream stream,
                     ObjectId treeId,
                     DataPath path,
                     ItemLoader referenceResolver);

    /// <summary>
    /// Transforms the <paramref name="node"/> into a text representation.
    /// </summary>
    /// <param name="node">The node to be converted.</param>
    /// <returns>The stream containing the text representation.</returns>
    Stream Serialize(Node node);

    /// <summary>
    /// Transforms the <paramref name="node"/> into a text representation.
    /// </summary>
    /// <param name="node">The node to be converted.</param>
    /// <param name="stream">The stream to write the text representation to.</param>
    void Serialize(Node node, Stream stream);

    /// <summary>
    /// Creates a RegEx representation of a value matching provided pattern.
    /// </summary>
    /// <param name="pattern">The pattern to be escaped by serializer.</param>
    /// <returns>Adapted RegEx pattern.</returns>
    string EscapeRegExPattern(string pattern);
}
