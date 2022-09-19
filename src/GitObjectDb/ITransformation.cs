using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using System.Collections.Generic;
using System.IO;

namespace GitObjectDb;

/// <summary>Applies a tree update on a <see cref="TreeDefinition"/>.</summary>
/// <param name="reference">The current tree.</param>
/// <param name="modules">The description of all modules being used by repository.</param>
/// <param name="serializer">The node serializer.</param>
/// <param name="dataBase">The data base.</param>
/// <param name="definition">The definition.</param>
internal delegate void ApplyUpdateTreeDefinition(Tree? reference,
                                                 ModuleCommands modules,
                                                 INodeSerializer serializer,
                                                 ObjectDatabase dataBase,
                                                 TreeDefinition definition);

/// <summary>Applies a tree update on a fast-insert file.</summary>
/// <param name="reference">The current tree.</param>
/// <param name="modules">The description of all modules being used by repository.</param>
/// <param name="serializer">The node serializer.</param>
/// <param name="data">The fast-insert file stream writer.</param>
/// <param name="commitIndex">The content of commit, point to data marks.</param>
internal delegate void ApplyUpdateFastInsert(Tree? reference,
                                             ModuleCommands modules,
                                             INodeSerializer serializer,
                                             StreamWriter data,
                                             IList<string> commitIndex);

/// <summary>Represents a node transformation.</summary>
public interface ITransformation
{
    /// <summary>Gets the path of modified item.</summary>
    public DataPath Path { get; }

    /// <summary>Gets new value of item.</summary>
    public ITreeItem? Item { get; }

    /// <summary>Gets the transformation description.</summary>
    public string Message { get; }
}

/// <summary>Represents a node transformation.</summary>
internal interface ITransformationInternal : ITransformation
{
    /// <summary>Gets the transformation that can be applied in the git database.</summary>
    ApplyUpdateTreeDefinition TreeTransformation { get; }

    /// <summary>Gets the transformation that can be applied through a fast-insert operation.</summary>
    ApplyUpdateFastInsert FastInsertTransformation { get; }
}
