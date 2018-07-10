using GitObjectDb.Models;
using GitObjectDb.Reflection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Represents a chunk change in a <see cref="IMetadataObject"/> while performing a merge.
    /// </summary>
    [DebuggerDisplay("Path = {Path}")]
    public class MetadataTreeMergeObjectAdd
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeMergeObjectAdd"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="branchNode">The branch node.</param>
        /// <exception cref="ArgumentNullException">
        /// path
        /// or
        /// branchNode
        /// </exception>
        public MetadataTreeMergeObjectAdd(string path, JObject branchNode)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            BranchNode = branchNode ?? throw new ArgumentNullException(nameof(branchNode));
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the branch node.
        /// </summary>
        public JObject BranchNode { get; }
    }
}
