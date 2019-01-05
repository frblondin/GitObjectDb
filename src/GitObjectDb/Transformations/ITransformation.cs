using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace GitObjectDb.Transformations
{
    /// <summary>
    /// Represents a transformation made to a repository object.
    /// </summary>
    public interface ITransformation
    {
        /// <summary>
        /// Gets the instance path.
        /// </summary>
        string Path { get; }
    }
}