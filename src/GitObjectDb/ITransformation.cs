using GitDotNet;
using GitObjectDb.Internal.Commands;
using System.Threading.Tasks;

namespace GitObjectDb;

internal delegate Task ApplyUpdate(
    TreeEntry? reference,
    ModuleCommands modules,
    INodeSerializer serializer,
    GitDotNet.ITransformationComposer composer);

/// <summary>Represents a node transformation.</summary>
public interface ITransformation
{
    /// <summary>Gets the path of modified item.</summary>
    public DataPath Path { get; }

    /// <summary>Gets new value of item.</summary>
    public TreeItem? Item { get; }

    /// <summary>Gets the transformation description.</summary>
    public string Message { get; }
}

/// <summary>Represents a node transformation.</summary>
internal interface ITransformationInternal : ITransformation
{
    /// <summary>Gets the transformation that can be applied in the git database.</summary>
    ApplyUpdate Action { get; }
}
