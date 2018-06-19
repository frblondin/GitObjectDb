using GitObjectDb.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Utils
{
    public interface IModelDataAccessor
    {
        ImmutableList<ChildPropertyInfo> ChildProperties { get; }
        ImmutableList<ModifiablePropertyInfo> ModifiableProperties { get; }
    }

    public class ModelDataAccessor : IModelDataAccessor
    {
        public Type Type { get; }
        readonly Lazy<ImmutableList<ChildPropertyInfo>> _childProperties;
        public ImmutableList<ChildPropertyInfo> ChildProperties => _childProperties.Value;

        readonly Lazy<ImmutableList<ModifiablePropertyInfo>> _modifiableProperties;
        public ImmutableList<ModifiablePropertyInfo> ModifiableProperties => _modifiableProperties.Value;

        public ModelDataAccessor(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            _childProperties = new Lazy<ImmutableList<ChildPropertyInfo>>(GetChildProperties);
            _modifiableProperties = new Lazy<ImmutableList<ModifiablePropertyInfo>>(GetModifiableProperties);
        }

        ImmutableList<ChildPropertyInfo> GetChildProperties() =>
            (from p in Type.GetProperties()
             let immutableType = p.PropertyType.GetInterfaces().Prepend(p.PropertyType).FirstOrDefault(t =>
                 t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IImmutableList<>))
             where immutableType != null
             select new ChildPropertyInfo(p, immutableType.GetGenericArguments()[0])).ToImmutableList();

        ImmutableList<ModifiablePropertyInfo> GetModifiableProperties() =>
            (from p in Type.GetProperties()
             where Attribute.IsDefined(p, typeof(ModifiableAttribute))
             select new ModifiablePropertyInfo(p)).ToImmutableList();
    }
}
