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
    public class ObjectRepositoryChunk
    {
#pragma warning disable CA1720 // Identifier contains type name
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectRepositoryChunk"/> class.
        /// </summary>
        /// <param name="object">The object.</param>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public ObjectRepositoryChunk(IModelObject @object, ModifiablePropertyInfo property, object value = null)
#pragma warning restore CA1720 // Identifier contains type name
        {
            Object = @object ?? throw new ArgumentNullException(nameof(@object));
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Value = value;
        }

#pragma warning disable CA1720 // Identifier contains type name
        /// <summary>
        /// The object.
        /// </summary>
        public IModelObject Object { get; }
#pragma warning restore CA1720 // Identifier contains type name

        /// <summary>
        /// The property.
        /// </summary>
        public ModifiablePropertyInfo Property { get; }

        /// <summary>
        /// The value of the chunk.
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// Gets whether the given <paramref name="chunk"/> <see cref="Value"/> is equal to the current value.
        /// </summary>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public bool HasSameValue(ObjectRepositoryChunk chunk)
        {
            if (chunk == null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            return EqualityComparer<object>.Default.Equals(Value, chunk.Value);
        }
    }
}
