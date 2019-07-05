using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GitObjectDb.Models.Compare
{
    /// <summary>
    /// Represents an object property value.
    /// </summary>
    [DebuggerDisplay("Object = {Object.Id}, Property = {Property.Name}, Value = {Value}")]
    public class ObjectRepositoryPropertyValue
    {
#pragma warning disable CA1720 // Identifier contains type name
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryPropertyValue"/> class.
        /// </summary>
        /// <param name="object">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public ObjectRepositoryPropertyValue(IModelObject @object, ModifiablePropertyInfo property, object value = null)
#pragma warning restore CA1720 // Identifier contains type name
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Value = value;
        }

#pragma warning disable CA1720 // Identifier contains type name
        /// <summary>
        /// Gets the object.
        /// </summary>
        public IModelObject Object { get; }
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// Gets the property.
        /// </summary>
        public ModifiablePropertyInfo Property { get; }

        /// <summary>
        /// Gets the value of the chunk.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets whether the given <paramref name="chunk"/> <see cref="Value"/> is equal to the current value.
        /// </summary>
        /// <param name="chunk">The chunk to compare to.</param>
        /// <returns><code>true</code> if chunks have same value. <code>false</code> otherwise.</returns>
        public bool HasSameValue(ObjectRepositoryPropertyValue chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            return EqualityComparer<object>.Default.Equals(Value, chunk.Value);
        }
    }
}
