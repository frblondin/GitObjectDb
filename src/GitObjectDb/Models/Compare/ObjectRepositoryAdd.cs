using GitObjectDb.Models;
using GitObjectDb.Reflection;
using Newtonsoft.Json.Linq;
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
    public class ObjectRepositoryAdd
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryAdd"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="node">The node.</param>
        /// <param name="parentId">The parent id.</param>
        /// <exception cref="ArgumentNullException">
        /// path
        /// or
        /// branchNode
        /// </exception>
        public ObjectRepositoryAdd(string path, JObject node, UniqueId parentId)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Node = node ?? throw new ArgumentNullException(nameof(node));
            ParentId = parentId;

            Id = node.GetValue(nameof(IModelObject.Id), StringComparison.OrdinalIgnoreCase).ToObject<UniqueId>();
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the node.
        /// </summary>
        public JObject Node { get; }

        /// <summary>
        /// Gets the parent unique id.
        /// </summary>
        public UniqueId ParentId { get; }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        public UniqueId Id { get; }
    }
}
