using LibGit2Sharp;

namespace GitObjectDb;

/// <summary>Provides a description of the parameters that should be used to load an item.</summary>
public record DataLoadParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataLoadParameters"/> class.
    /// </summary>
    /// <param name="path">The path of loaded item.</param>
    /// <param name="treeId">The id of the tree from which the blob is loaded.</param>
    public DataLoadParameters(DataPath path, ObjectId treeId)
    {
        Path = path;
        TreeId = treeId;
    }

    /// <summary>Gets the path of loaded item.</summary>
    public DataPath Path { get; }

    /// <summary>Gets the id of the tree from which the blob is loaded.</summary>
    public ObjectId TreeId { get; }
}
