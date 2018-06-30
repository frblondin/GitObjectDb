using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Reflection
{
    public class ChildPropertyInfo
    {
        public PropertyInfo Property { get; }
        public Type ItemType { get; }
        public Func<IMetadataObject, IEnumerable<IMetadataObject>> Accessor { get; }
        public Func<IMetadataObject, bool> ShouldVisitChildren { get; }

        public ChildPropertyInfo(PropertyInfo property, Type itemType)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
            Accessor = CreateGetter(property).Compile();
            ShouldVisitChildren = GetShouldVisitChildrenGetter(property).Compile();
        }

        Expression<Func<IMetadataObject, IEnumerable<IMetadataObject>>> CreateGetter(PropertyInfo property)
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

        Expression<Func<IMetadataObject, bool>> GetShouldVisitChildrenGetter(PropertyInfo property)
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

        public bool Matches(string name) => Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
