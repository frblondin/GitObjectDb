using GitObjectDb.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Provides information to manage child properties.
    /// </summary>
    [DebuggerDisplay("Name = {Name}, ItemType = {ItemType}")]
    public class ChildPropertyInfo
    {
        private readonly PropertyNameAttribute _childPropertyNameAttribute;

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
            _childPropertyNameAttribute = property.GetCustomAttribute<PropertyNameAttribute>(true);
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
        public Func<IModelObject, IEnumerable<IModelObject>> Accessor { get; }

        /// <summary>
        /// Gets whether children should be visited.
        /// </summary>
        public Func<IModelObject, bool> ShouldVisitChildren { get; }

        /// <summary>
        /// Gets the name of the child property container.
        /// </summary>
        public string Name => Property.Name;

        /// <summary>
        /// Gets the name of the folder.
        /// </summary>
        public string FolderName => _childPropertyNameAttribute?.Name ?? Name;

        private static Expression<Func<IModelObject, IEnumerable<IModelObject>>> CreateGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(IModelObject), "instance");
            return Expression.Lambda<Func<IModelObject, IEnumerable<IModelObject>>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(instanceParam, property.DeclaringType),
                        property),
                    typeof(IEnumerable<IModelObject>)),
                instanceParam);
        }

        private static Expression<Func<IModelObject, bool>> GetShouldVisitChildrenGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(IModelObject), "instance");
            var lazyChildren = Expression.Convert(
                Expression.Property(Expression.Convert(instanceParam, property.DeclaringType), property),
                typeof(ILazyChildren));
            return Expression.Lambda<Func<IModelObject, bool>>(
                Expression.OrElse(
                    Expression.Property(lazyChildren, nameof(ILazyChildren.AreChildrenLoaded)),
                    Expression.Property(lazyChildren, nameof(ILazyChildren.ForceVisit))),
                instanceParam);
        }
    }
}
