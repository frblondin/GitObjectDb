using GitObjectDb.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Provides information to properties decorated with the <see cref="ModifiableAttribute"/> attribute.
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    public class ModifiablePropertyInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModifiablePropertyInfo"/> class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <exception cref="ArgumentNullException">property</exception>
        public ModifiablePropertyInfo(PropertyInfo property)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Accessor = CreateGetter(property).Compile();
            IsLink = typeof(ILazyLink).IsAssignableFrom(Property.PropertyType);
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Gets the property value accessor.
        /// </summary>
        public Func<IModelObject, object> Accessor { get; }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name => Property.Name;

        /// <summary>
        /// Gets a value indicating whether this property is of type <see cref="ILazyLink"/>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this property is a link; otherwise, <c>false</c>.
        /// </value>
        public bool IsLink { get; }

        static Expression<Func<IModelObject, object>> CreateGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(IModelObject), "instance");
            return Expression.Lambda<Func<IModelObject, object>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(instanceParam, property.DeclaringType),
                        property),
                    typeof(object)),
                instanceParam);
        }

        /// <summary>
        /// Gets whether two objects store the same value.
        /// </summary>
        /// <param name="old">The old.</param>
        /// <param name="new">The new.</param>
        /// <returns><code>true</code> is the objects contain the same property value.</returns>
        /// <exception cref="ArgumentNullException">
        /// old
        /// or
        /// new
        /// </exception>
        public bool AreSame(IModelObject old, IModelObject @new)
        {
            if (old == null)
            {
                throw new ArgumentNullException(nameof(old));
            }
            if (@new == null)
            {
                throw new ArgumentNullException(nameof(@new));
            }

            var oldValue = Accessor(old);
            var newValue = Accessor(@new);
            return oldValue == newValue || (oldValue?.Equals(newValue) ?? false);
        }
    }
}
