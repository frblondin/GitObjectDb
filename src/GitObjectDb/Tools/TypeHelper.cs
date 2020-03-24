using System;
using System.Collections.Generic;

namespace GitObjectDb.Tools
{
    internal static class TypeHelper
    {
        internal static bool IsEnumerableType(Type enumerableType) =>
            FindGenericType(typeof(IEnumerable<>), enumerableType) != null;

        internal static bool IsKindOfGeneric(Type type, Type definition) =>
            FindGenericType(definition, type) != null;

        internal static Type GetElementType(Type enumerableType)
        {
            var type = FindGenericType(typeof(IEnumerable<>), enumerableType);
            if (type != null)
            {
                return type.GetGenericArguments()[0];
            }
            return enumerableType;
        }

        internal static Type? FindGenericType(Type definition, Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == definition)
                {
                    return type;
                }
                if (definition.IsInterface)
                {
                    foreach (var @interface in type.GetInterfaces())
                    {
                        var genericType = FindGenericType(definition, @interface);
                        if (genericType != null)
                        {
                            return genericType;
                        }
                    }
                }
                type = type.BaseType;
            }
            return null;
        }

        internal static bool IsNullableType(Type type) =>
            type != null &&
            type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(Nullable<>);

        internal static Type GetNonNullableType(Type type) =>
            IsNullableType(type) ?
            type.GetGenericArguments()[0] :
            type;
    }
}
