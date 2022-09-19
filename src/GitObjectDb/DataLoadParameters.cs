using LibGit2Sharp;
using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace GitObjectDb;

/// <summary>Provides a description of the parameters that should be used to load an item.</summary>
[Serializable]
public record DataLoadParameters : ISerializable
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DataLoadParameters"/> class.
    /// </summary>
    /// <param name="info">Data needed to deserialize parameters.</param>
    /// <param name="context">Caller-defined context.</param>
    protected DataLoadParameters(SerializationInfo info, StreamingContext context)
    {
        if (info is null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        Path = DataPath.Parse(info.GetString(nameof(Path)));
        TreeId = new ObjectId(info.GetString(nameof(TreeId)));
    }

    /// <summary>Gets the path of loaded item.</summary>
    public DataPath Path { get; }

    /// <summary>Gets the id of the tree from which the blob is loaded.</summary>
    public ObjectId TreeId { get; }

    /// <summary>
    /// Populates a <see cref="SerializationInfo" /> with the data needed to
    /// serialize the target object.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo" /> to populate with data.</param>
    /// <param name="context">The destination (see <see cref="StreamingContext" /> ) for this
    /// serialization.</param>
    [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
    protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info is null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        info.AddValue(nameof(Path), Path.FileName);
        info.AddValue(nameof(TreeId), TreeId.RawId);
    }

    /// <inheritdoc/>
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        GetObjectData(info, context);
    }
}
