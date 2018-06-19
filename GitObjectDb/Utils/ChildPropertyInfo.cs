using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Utils
{
    public class ChildPropertyInfo
    {
        public PropertyInfo Property { get; }
        public Type ItemType { get; }
        public Func<IMetadataObject, System.Collections.IList> Accessor { get; }
        public Func<IMetadataObject, bool> ShouldVisitChildren { get; }

        public ChildPropertyInfo(PropertyInfo property, Type itemType)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
            Accessor = CreateGetter(property).Compile();
            ShouldVisitChildren = GetShouldVisitChildrenGetter(property).Compile();
        }

        Expression<Func<IMetadataObject, System.Collections.IList>> CreateGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(IMetadataObject), "instance");
            return Expression.Lambda<Func<IMetadataObject, System.Collections.IList>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Convert(instanceParam, property.DeclaringType),
                        property),
                    typeof(System.Collections.IList)),
                instanceParam);
        }

        Expression<Func<IMetadataObject, bool>> GetShouldVisitChildrenGetter(PropertyInfo property)
        {
            var instanceParam = Expression.Parameter(typeof(IMetadataObject), "instance");
            var expectedFieldName = $"_{property.Name}";
            var field = property.DeclaringType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.Name.Equals(expectedFieldName, StringComparison.OrdinalIgnoreCase)) ??
                throw new NotSupportedException($"Field named '{expectedFieldName}' expected.");
            var lazyChildrenVar = Expression.Variable(typeof(ILazyChildren), "lazyChildren");
            return Expression.Lambda<Func<IMetadataObject, bool>>(
                Expression.Block(
                    new[] { lazyChildrenVar },
                    Expression.Assign(
                        lazyChildrenVar,
                        Expression.Field(Expression.Convert(instanceParam, property.DeclaringType), field)),
                    Expression.OrElse(
                        Expression.Property(lazyChildrenVar, nameof(ILazyChildren.AreChildrenLoaded)),
                        Expression.Property(lazyChildrenVar, nameof(ILazyChildren.ForceVisit)))),
                instanceParam);
        }

        public bool Matches(string name) => Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
