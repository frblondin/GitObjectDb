using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Reflection
{
    public class ModifiablePropertyInfo
    {
        public PropertyInfo Property { get; }
        public Func<IMetadataObject, object> Accessor { get; }

        public ModifiablePropertyInfo(PropertyInfo property)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
            Accessor = CreateGetter(property).Compile();
        }

        public bool AreSame(IMetadataObject old, IMetadataObject @new)
        {
            if (old == null) throw new ArgumentNullException(nameof(old));
            if (@new == null) throw new ArgumentNullException(nameof(@new));

            var oldValue = Accessor(old);
            var newValue = Accessor(@new);
            return oldValue == newValue || (oldValue?.Equals(newValue) ?? false);
        }

        Expression<Func<IMetadataObject, object>> CreateGetter(PropertyInfo property)
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

        public bool Matches(string name) => Property.Name.Equals(name, StringComparison.OrdinalIgnoreCase);
    }
}
