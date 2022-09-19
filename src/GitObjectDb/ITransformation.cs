using LibGit2Sharp;
using System.Collections.Generic;
using System.IO;

namespace GitObjectDb;

/// <summary>Applies a tree update on a <see cref="TreeDefinition"/>.</summary>
/// <param name="dataBase">The data base.</param>
/// <param name="definition">The definition.</param>
/// <param name="reference">The reference.</param>
public delegate void ApplyUpdateTreeDefinition(ObjectDatabase dataBase, TreeDefinition definition, Tree? reference);

/// <summary>Applies a tree update on a fast-insert file.</summary>
/// <param name="reference">The current tree.</param>
/// <param name="data">The fast-insert file stream writer.</param>
/// <param name="commitIndex">The content of commit, point to data marks.</param>
public delegate void ApplyUpdateFastInsert(Tree? reference, StreamWriter data, IList<string> commitIndex);

/// <summary>Represents a node transformation.</summary>
public interface ITransformation
{
    /// <summary>Gets the transformation that can be applied in the git database.</summary>
    ApplyUpdateTreeDefinition TreeTransformation { get; }

    /// <summary>Gets the transformation that can be applied through a fast-insert operation.</summary>
    ApplyUpdateFastInsert FastInsertTransformation { get; }

    /// <summary>Gets the transformation description.</summary>
    public string Message { get; }
}
