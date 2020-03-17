namespace GitObjectDb
{
    /// <summary>
    /// Represents a blob (<see cref="Node"/>, <see cref="Resource"/>...) managed by
    /// the engine.
    /// </summary>
    public interface ITreeItem
    {
        /// <summary>Gets the blob path.</summary>
        DataPath Path { get; }
    }

    internal interface ITreeItemInternal
    {
        DataPath Path { get; set; }
    }
}