using GitObjectDb.Models;
using System;
using System.Diagnostics;

namespace GitObjectDb.Reflection
{
    internal partial class PredicateReflector
    {
        /// <summary>
        /// Provides information about a child change.
        /// </summary>
        [DebuggerDisplay("{Type} {Child.Id}")]
        internal class ChildChange
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ChildChange" /> class.
            /// </summary>
            /// <param name="child">The child.</param>
            /// <param name="type">The type.</param>
            public ChildChange(IModelObject child, ChildChangeType type)
            {
                Type = type;
                Child = child ?? throw new ArgumentNullException(nameof(child));
            }

            /// <summary>
            /// Gets the change type.
            /// </summary>
            internal ChildChangeType Type { get; }

            /// <summary>
            /// Gets concerned child.
            /// </summary>
            internal IModelObject Child { get; }
        }
    }
}
