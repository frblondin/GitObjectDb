using System;

namespace GitObjectDb.Tools
{
    internal static class TypeHelper
    {
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
    }
}
