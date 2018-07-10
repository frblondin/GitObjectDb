using GitObjectDb.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Provides information to properties decorated with the <see cref="ModifiableAttribute"/> attribute.
    /// </summary>
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
            Setter = CreateSetter(property.DeclaringType.GetProperty(property.Name)).Compile();
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Gets the property value accessor.
        /// </summary>
        public Func<IMetadataObject, object> Accessor { get; }

        /// <summary>
        /// Gets the property value setter.
        /// </summary>
        public Action<IMetadataObject, object> Setter { get; }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name => Property.Name;

        static Expression<Func<IMetadataObject, object>> CreateGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(IMetadataObject), "instance");
            return Expression.Lambda<Func<IMetadataObject, object>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(instanceParam, property.DeclaringType),
                        property),
                    typeof(object)),
                instanceParam);
        }

        static Expression<Action<IMetadataObject, object>> CreateSetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(IMetadataObject), "instance");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var propertySetter = property.GetSetMethod(true) ??
                throw new NotSupportedException($"No public/private setter could be found for property {property}.");
            return Expression.Lambda<Action<IMetadataObject, object>>(
                Expression.Call(
                    Expression.Convert(instanceParam, property.DeclaringType),
                    propertySetter,
                    Expression.Convert(
                        valueParam,
                        property.PropertyType)),
                instanceParam, valueParam);
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
        public bool AreSame(IMetadataObject old, IMetadataObject @new)
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
