using LibGit2Sharp;
using System;

namespace GitObjectDb
{
    /// <summary>Represents a node transformation.</summary>
    public interface ITransformation
    {
        /// <summary>Gets the transformation that can be applied in the git database.</summary>
        ApplyUpdateTreeDefinition TreeTransformation { get; }

        /// <summary>Gets the transformation description.</summary>
        public string Message { get; }
    }
}
