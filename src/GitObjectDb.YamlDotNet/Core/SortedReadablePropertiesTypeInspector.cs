using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace GitObjectDb.YamlDotNet.Core;
internal class SortedReadablePropertiesTypeInspector : TypeInspectorSkeleton
{
    private readonly ITypeResolver _typeResolver;

    public SortedReadablePropertiesTypeInspector(ITypeInspector inner, ITypeResolver typeResolver)
    {
        if (inner is not ReadablePropertiesTypeInspector)
        {
            throw new NotSupportedException(
                $"{nameof(SortedReadablePropertiesTypeInspector)} is intended to " +
                $"replace {nameof(ReadablePropertiesTypeInspector)} inspector. " +
                $"{inner.GetType().Name} was not expected.");
        }
        _typeResolver = typeResolver;
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container) => type
        .GetProperties()
        .Where(IsValidProperty)
        .OrderBy(GetOrder)
        .Select(p => new ReflectionPropertyDescriptor(p, _typeResolver));

    private static bool IsValidProperty(PropertyInfo property) =>
        property.CanRead &&
        property.GetGetMethod(true)!.GetParameters().Length == 0;

    private static (int HierarchyDepth, string Name) GetOrder(PropertyInfo property) =>
        (GetHierarchyDepth(property.DeclaringType), property.Name);

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

    private sealed class ReflectionPropertyDescriptor : IPropertyDescriptor
    {
        private readonly PropertyInfo _propertyInfo;
        private readonly ITypeResolver _typeResolver;

        public ReflectionPropertyDescriptor(PropertyInfo propertyInfo, ITypeResolver typeResolver)
        {
            _propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
            _typeResolver = typeResolver ?? throw new ArgumentNullException(nameof(typeResolver));
            ScalarStyle = ScalarStyle.Any;
        }

        public string Name => _propertyInfo.Name;

        public Type Type => _propertyInfo.PropertyType;

        public Type? TypeOverride { get; set; }

        public int Order { get; set; }

        public bool CanWrite => _propertyInfo.CanWrite;

        public ScalarStyle ScalarStyle { get; set; }

        public void Write(object target, object? value)
        {
            _propertyInfo.SetValue(target, value, null);
        }

        public T GetCustomAttribute<T>()
            where T : Attribute
        {
            var attributes = Attribute.GetCustomAttributes(_propertyInfo, typeof(T));
            return (T)attributes.FirstOrDefault();
        }

        public IObjectDescriptor Read(object target)
        {
            var propertyValue = _propertyInfo.GetValue(target, null);
            var actualType = TypeOverride ?? _typeResolver.Resolve(Type, propertyValue);
            return new ObjectDescriptor(propertyValue, actualType, Type, ScalarStyle);
        }
    }
}
