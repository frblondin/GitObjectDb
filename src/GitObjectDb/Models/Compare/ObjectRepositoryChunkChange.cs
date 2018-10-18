using GitObjectDb.Attributes;
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
    [DebuggerDisplay("Property = {Property.Name}, Path = {Path}")]
    [ExcludeFromGuardForNull]
    public class ObjectRepositoryChunkChange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryChunkChange"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="property">The property.</param>
        /// <param name="ancestor">The ancestor.</param>
        /// <param name="ancestorValue">The ancestor value.</param>
        /// <param name="theirs">Their node.</param>
        /// <param name="theirsValue">Their value.</param>
        /// <param name="ours">Our node.</param>
        /// <param name="oursValue">Our value.</param>
        /// <exception cref="ArgumentNullException">
        /// path
        /// or
        /// property
        /// or
        /// ancestor
        /// or
        /// ancestorValue
        /// or
        /// theirs
        /// or
        /// theirsValue
        /// or
        /// ours
        /// or
        /// oursValue
        /// </exception>
        public ObjectRepositoryChunkChange(string path, ModifiablePropertyInfo property, JObject ancestor, JToken ancestorValue, JObject theirs, JToken theirsValue, JObject ours, JToken oursValue)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Ancestor = ancestor ?? throw new ArgumentNullException(nameof(ancestor));
            AncestorValue = ancestorValue ?? throw new ArgumentNullException(nameof(ancestorValue));
            Theirs = theirs ?? throw new ArgumentNullException(nameof(theirs));
            TheirsValue = theirsValue ?? throw new ArgumentNullException(nameof(theirsValue));
            Ours = ours ?? throw new ArgumentNullException(nameof(ours));
            OursValue = oursValue ?? throw new ArgumentNullException(nameof(oursValue));

            Id = ancestor[nameof(IModelObject.Id)].ToObject<UniqueId>();

            if (!IsInConflict)
            {
                MergeValue = TheirsValue;
            }
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the ancestor.
        /// </summary>
        public JObject Ancestor { get; }

        /// <summary>
        /// Gets their node.
        /// </summary>
        public JObject Theirs { get; }

        /// <summary>
        /// Gets our node.
        /// </summary>
        public JObject Ours { get; }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public ModifiablePropertyInfo Property { get; }

        /// <summary>
        /// Gets the ancestor value.
        /// </summary>
        public JToken AncestorValue { get; }

        /// <summary>
        /// Gets their value.
        /// </summary>
        public JToken TheirsValue { get; }

        /// <summary>
        /// Gets our value.
        /// </summary>
        public JToken OursValue { get; }

        /// <summary>
        /// Gets a value indicating whether the change is in conflict change.
        /// </summary>
        public bool IsInConflict => MergeValue == null && WasInConflict;

        /// <summary>
        /// Gets a value indicating whether the change was in conflict change.
        /// </summary>
        public bool WasInConflict => !JToken.DeepEquals(AncestorValue, OursValue) && !JToken.DeepEquals(TheirsValue, OursValue);

        /// <summary>
        /// Gets the merge value.
        /// </summary>
        public JToken MergeValue { get; private set; }

        /// <summary>
        /// Gets the unique id.
        /// </summary>
        public UniqueId Id { get; }

        /// <summary>
        /// Resolves the conflict by assigning the merge value.
        /// </summary>
        /// <param name="value">The merge value.</param>
        public virtual void Resolve(JToken value)
        {
            if (MergeValue != null)
            {
                throw new GitObjectDbException("There is no conflict on this node.");
            }

            MergeValue = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
