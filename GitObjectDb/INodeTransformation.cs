using LibGit2Sharp;
using System;

namespace GitObjectDb
{
    /// <summary>Represents a node transformation.</summary>
    public interface INodeTransformation
    {
        /// <summary>Gets the transformation that can be applied in the git database.</summary>
        Action<ObjectDatabase, TreeDefinition> Transformation { get; }
    }
}
