using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb
{
    /// <summary>
    /// Applies a tree update on a <see cref="TreeDefinition"/>.
    /// </summary>
    /// <param name="dataBase">The data base.</param>
    /// <param name="definition">The definition.</param>
    /// <param name="reference">The reference.</param>
    public delegate void ApplyUpdateTreeDefinition(ObjectDatabase dataBase, TreeDefinition definition, Tree? reference);
}
