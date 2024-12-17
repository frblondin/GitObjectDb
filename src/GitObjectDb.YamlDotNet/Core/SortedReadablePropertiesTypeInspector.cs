using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace GitObjectDb.YamlDotNet.Core;
internal class SortedReadablePropertiesTypeInspector : ReadablePropertiesTypeInspector
{
    private readonly ITypeResolver _typeResolver;
    private readonly BindingFlags _propertyBindingFlags;

    public SortedReadablePropertiesTypeInspector(ITypeResolver typeResolver, bool includeNonPublicProperties)
        : base(typeResolver, includeNonPublicProperties)
    {
        _typeResolver = typeResolver;
        _propertyBindingFlags = BindingFlags.Instance |
            BindingFlags.Public |
            (includeNonPublicProperties ? BindingFlags.NonPublic : 0);
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) =>
        from p in type.GetProperties(_propertyBindingFlags)
        where IsValidProperty(p)
        orderby GetOrder(p)
        select new ReflectionPropertyDescriptor(p, _typeResolver);

    private static bool IsValidProperty(PropertyInfo property) =>
        property.CanRead &&
        property.GetGetMethod(true)!.GetParameters().Length == 0;

    private static (int HierarchyDepth, string Name) GetOrder(PropertyInfo property) =>
        (GetHierarchyDepth(property.DeclaringType!), property.Name);

    private static int GetHierarchyDepth(Type type)
    {
        var depth = 0;
        var parent = type.BaseType;
        while (parent is not null)
        {
            depth++;
            parent = parent.BaseType;
        }
        return depth;
    }
}
