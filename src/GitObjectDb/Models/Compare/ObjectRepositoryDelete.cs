using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models.Compare
{
    /// <summary>
    /// Represents a chunk change in a <see cref="IModelObject"/> while performing a merge.
    /// </summary>
    [DebuggerDisplay("Path = {Path}")]
    public class ObjectRepositoryDelete
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryDelete"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="id">The object id.</param>
        /// <exception cref="ArgumentNullException">
        /// path
        /// or
        /// branchNode
        /// </exception>
        public ObjectRepositoryDelete(string path, UniqueId id)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Id = id;
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        public UniqueId Id { get; }
    }
}
