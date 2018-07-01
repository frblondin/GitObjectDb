using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Provides information to manage child properties.
    /// </summary>
    public class ChildPropertyInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildPropertyInfo"/> class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="itemType">Type of the item.</param>
        /// <exception cref="ArgumentNullException">
        /// property
        /// or
        /// itemType
        /// </exception>
        public ChildPropertyInfo(PropertyInfo property, Type itemType)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
            Accessor = CreateGetter(property).Compile();
            ShouldVisitChildren = GetShouldVisitChildrenGetter(property).Compile();
        }

        /// <summary>
        /// Gets the property.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Gets the type of the children.
        /// </summary>
        public Type ItemType { get; }

        /// <summary>
        /// Gets the children accessor.
        /// </summary>
        public Func<IMetadataObject, IEnumerable<IMetadataObject>> Accessor { get; }

        /// <summary>
        /// Gets whether children should be visited.
        /// </summary>
        public Func<IMetadataObject, bool> ShouldVisitChildren { get; }

        static Expression<Func<IMetadataObject, IEnumerable<IMetadataObject>>> CreateGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(IMetadataObject), "instance");
            return Expression.Lambda<Func<IMetadataObject, IEnumerable<IMetadataObject>>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(instanceParam, property.DeclaringType),
                        property),
                    typeof(IEnumerable<IMetadataObject>)),
                instanceParam);
        }

        static Expression<Func<IMetadataObject, bool>> GetShouldVisitChildrenGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(IMetadataObject), "instance");
            var lazyChildren = Expression.Convert(
                Expression.Property(Expression.Convert(instanceParam, property.DeclaringType), property),
                typeof(ILazyChildren));
            return Expression.Lambda<Func<IMetadataObject, bool>>(
                Expression.OrElse(
                    Expression.Property(lazyChildren, nameof(ILazyChildren.AreChildrenLoaded)),
                    Expression.Property(lazyChildren, nameof(ILazyChildren.ForceVisit))),
                instanceParam);
        }

        /// <summary>
        /// Gets whether this instance has the same case insensitive name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><code>true</code> is the names are matching.</returns>
        public bool Matches(string name) =>
            Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
