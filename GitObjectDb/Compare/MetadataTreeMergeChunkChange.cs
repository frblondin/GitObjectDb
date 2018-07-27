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
    [DebuggerDisplay("Property = {Property.Name}, Path = {Path}")]
    public class MetadataTreeMergeChunkChange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataTreeMergeChunkChange"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mergeBaseNode">The merge base node.</param>
        /// <param name="branchNode">The branch node.</param>
        /// <param name="headNode">The head node.</param>
        /// <param name="property">The property.</param>
        /// <param name="mergeBaseValue">The merge base value.</param>
        /// <param name="branchValue">The branch value.</param>
        /// <param name="headValue">The head value.</param>
        /// <exception cref="ArgumentNullException">
        /// path
        /// or
        /// mergeBaseNode
        /// or
        /// branchNode
        /// or
        /// headNode
        /// or
        /// property
        /// or
        /// value
        /// </exception>
        public MetadataTreeMergeChunkChange(string path, JObject mergeBaseNode, JObject branchNode, JObject headNode, ModifiablePropertyInfo property, JToken mergeBaseValue, JToken branchValue, JToken headValue)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            MergeBaseNode = mergeBaseNode ?? throw new ArgumentNullException(nameof(mergeBaseNode));
            BranchNode = branchNode ?? throw new ArgumentNullException(nameof(branchNode));
            HeadNode = headNode ?? throw new ArgumentNullException(nameof(headNode));
            Property = property ?? throw new ArgumentNullException(nameof(property));
            MergeBaseValue = mergeBaseValue ?? throw new ArgumentNullException(nameof(mergeBaseValue));
            BranchValue = branchValue ?? throw new ArgumentNullException(nameof(branchValue));
            HeadValue = headValue ?? throw new ArgumentNullException(nameof(headValue));

            if (!IsInConflict)
            {
                MergeValue = BranchValue;
            }
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the merge base node.
        /// </summary>
        public JObject MergeBaseNode { get; }

        /// <summary>
        /// Gets the branch node.
        /// </summary>
        public JObject BranchNode { get; }

        /// <summary>
        /// Gets the head node.
        /// </summary>
        public JObject HeadNode { get; }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public ModifiablePropertyInfo Property { get; }

        /// <summary>
        /// Gets the merge base value.
        /// </summary>
        public JToken MergeBaseValue { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public JToken BranchValue { get; }

        /// <summary>
        /// Gets the head value.
        /// </summary>
        public JToken HeadValue { get; }

        /// <summary>
        /// Gets a value indicating whether the change is in conflict change.
        /// </summary>
        public bool IsInConflict => MergeValue == null && WasInConflict;

        /// <summary>
        /// Gets a value indicating whether the change was in conflict change.
        /// </summary>
        public bool WasInConflict => !JToken.DeepEquals(MergeBaseValue, HeadValue) && !JToken.DeepEquals(BranchValue, HeadValue);

        /// <summary>
        /// Gets the merge value.
        /// </summary>
        public JToken MergeValue { get; private set; }

        /// <summary>
        /// Resolves the conflict by assigning the merge value.
        /// </summary>
        /// <param name="value">The merge value.</param>
        public void Resolve(JToken value)
        {
            if (MergeValue != null)
            {
                throw new NotSupportedException("There is no conflict on this node.");
            }

            MergeValue = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
