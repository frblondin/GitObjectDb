using GitObjectDb.Attributes;
using GitObjectDb.Reflection;
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
    [DebuggerDisplay("Property = {Property.Name}, Path = {Path}, IsInConflict = {IsInConflict}, WasInConflict = {WasInConflict}")]
    [ExcludeFromGuardForNull]
    public class ObjectRepositoryChunkChange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryChunkChange"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="property">The property.</param>
        /// <param name="ancestor">The ancestor.</param>
        /// <param name="theirs">Their node.</param>
        /// <param name="ours">Our node.</param>
        public ObjectRepositoryChunkChange(string path, ModifiablePropertyInfo property, ObjectRepositoryChunk ancestor, ObjectRepositoryChunk theirs, ObjectRepositoryChunk ours)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Ancestor = ancestor ?? throw new ArgumentNullException(nameof(ancestor));
            Theirs = theirs ?? throw new ArgumentNullException(nameof(theirs));
            Ours = ours ?? throw new ArgumentNullException(nameof(ours));
            WasInConflict = !ancestor.HasSameValue(ours) && !theirs.HasSameValue(ours);

            Id = ancestor.Object.Id;

            if (!IsInConflict)
            {
                MergeValue = Theirs.Value;
            }
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the ancestor.
        /// </summary>
        public ObjectRepositoryChunk Ancestor { get; }

        /// <summary>
        /// Gets their node.
        /// </summary>
        public ObjectRepositoryChunk Theirs { get; }

        /// <summary>
        /// Gets our node.
        /// </summary>
        public ObjectRepositoryChunk Ours { get; }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public ModifiablePropertyInfo Property { get; }

        /// <summary>
        /// Gets a value indicating whether the change is in conflict change.
        /// </summary>
        public bool IsInConflict => MergeValue == null && WasInConflict;

        /// <summary>
        /// Gets a value indicating whether the change was in conflict change.
        /// </summary>
        public bool WasInConflict { get; }

        /// <summary>
        /// Gets the merge value.
        /// </summary>
        public object MergeValue { get; private set; }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        public UniqueId Id { get; }

        /// <summary>
        /// Resolves the conflict by assigning the merge value.
        /// </summary>
        /// <param name="value">The merge value.</param>
        public virtual void Resolve(object value)
        {
            if (MergeValue != null)
            {
                throw new GitObjectDbException("There is no conflict on this node.");
            }

            MergeValue = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
