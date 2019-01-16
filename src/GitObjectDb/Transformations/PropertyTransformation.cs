using GitObjectDb.Attributes;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Transformations
{
    /// <inheritdoc/>
    internal partial class PropertyTransformation : ITransformation
    {
        internal PropertyTransformation(IModelObject instance, Expression propertyPicker, object value = null)
            : this(instance.Id, instance.GetDataPath(), PropertyVisitor.ExtractProperty(propertyPicker), value)
        {
        }

        internal PropertyTransformation(UniqueId instanceId, string path, PropertyInfo property, object value = null)
        {
            InstanceId = instanceId;
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Value = value;

            if (!Attribute.IsDefined(property, typeof(ModifiableAttribute)))
            {
                throw new GitObjectDbException($"Member expressions should be decorated with {nameof(ModifiableAttribute)} attribute.");
            }
        }

        /// <summary>
        /// Gets the instance <see cref="UniqueId"/>.
        /// </summary>
        public UniqueId InstanceId { get; }

        /// <inheritdoc/>
        public string Path { get; }

        /// <summary>
        /// Gets the property that will have the value modified.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string PropertyName => Property.Name;

        /// <summary>
        /// Gets the value to update the property.
        /// </summary>
        public object Value { get; }
    }
}
