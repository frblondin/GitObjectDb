using GitObjectDb.Reflection;
using System;

namespace GitObjectDb.Models.Compare
{
    /// <summary>
    /// Represents an object property value.
    /// </summary>
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
    }
}
