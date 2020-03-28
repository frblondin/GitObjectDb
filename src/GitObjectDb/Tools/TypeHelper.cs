using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Tools
{
    internal static class TypeHelper
    {
        private static readonly ConcurrentDictionary<Type, IEnumerable<Type>> _derivedTypesIncludingSelfCache = new();

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

        internal static Type GetPublicLinqType(Type t)
        {
            if (t.IsGenericType &&
                t.GetGenericTypeDefinition() == typeof(Lookup<,>).GetNestedType("Grouping", BindingFlags.Public | BindingFlags.NonPublic))
            {
                return typeof(IGrouping<,>).MakeGenericType(t.GetGenericArguments());
            }
            if (!t.IsNestedPrivate)
            {
                return t;
            }
            foreach (var type in t.GetInterfaces())
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return type;
                }
            }
            return typeof(IEnumerable).IsAssignableFrom(t) ?
                typeof(IEnumerable) :
                t;
        }

        internal static int GetAssemblyDelimiterIndex(string fullTypeName)
        {
            var num = 0;
            for (var i = 0; i < fullTypeName.Length; i++)
            {
                switch (fullTypeName[i])
                {
                    // Manage nested generic type args, if any
                    case '[':
                        num++;
                        break;
                    case ']':
                        num--;
                        break;
                    case ',' when num == 0:
                        return i;
                }
            }
            throw new NotSupportedException("Assembly delimiter could not be found in full type name.");
        }

        internal static IEnumerable<Type> GetDerivedTypesIncludingSelf(Type type)
        {
            return _derivedTypesIncludingSelfCache.GetOrAdd(type, SearchForDerivedTypesIncludingSelf);

            IEnumerable<Type> SearchForDerivedTypesIncludingSelf(Type type) =>
                (from a in AppDomain.CurrentDomain.GetAssemblies()
                 from t in a.GetTypes()
                 where type.IsAssignableFrom(t)
                 select t).ToList().AsReadOnly();
        }
    }
}
