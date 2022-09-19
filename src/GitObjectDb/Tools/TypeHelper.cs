using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Tools;

/// <summary>Provides helpers for <see cref="Type"/>.</summary>
internal static class TypeHelper
{
    private static readonly ConcurrentDictionary<string, Type> _typeBindingCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Converts a <paramref name="type"/> to its <see cref="string"/> equivalent.</summary>
    /// <param name="type">The <see cref="Type"/> to be converted.</param>
    /// <returns>The <see cref="string"/> representation of <paramref name="type"/>.</returns>
    public static string BindToName(Type type) => $"{type.FullName}, {type.Assembly.FullName}";

    /// <summary>Converts a <see cref="string"/> representation to its corresponding <see cref="Type"/>.</summary>
    /// <param name="fullTypeName">The <see cref="string"/> representation of the <see cref="Type"/> to be converted.</param>
    /// <returns>The <see cref="Type"/> corresponding to <paramref name="fullTypeName"/>.</returns>
    public static Type BindToType(string fullTypeName)
    {
        return _typeBindingCache.GetOrAdd(fullTypeName, ParseType);

        Type ParseType(string name)
        {
            var index = GetAssemblyDelimiterIndex(name);

            var assemblyFullName = name.Substring(index + 1).Trim();
            var assemblyName = GetAssemblyName(assemblyFullName);

            // Try first to retrieve loaded assembly with no strong version check
            // ... and load assembly if none could be found
            var assembly =
                AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
                    a => !a.IsDynamic && a.GetName().Name == assemblyName) ??
                Assembly.Load(assemblyFullName);

            var typeName = name.Substring(0, index).Trim();
            var type = assembly.GetType(typeName);

            return type;
        }

        string GetAssemblyName(string fullName)
        {
            var index = fullName.IndexOf(',');
            return index == -1 ?
                fullName :
                fullName.Substring(0, index).Trim();
        }
    }

    internal static int GetAssemblyDelimiterIndex(string fullTypeName)
    {
        var level = 0;
        for (var i = 0; i < fullTypeName.Length; i++)
        {
            switch (fullTypeName[i])
            {
                // Manage nested generic type args, if any
                case '[':
                    level++;
                    break;
                case ']':
                    level--;
                    break;
                case ',' when level == 0:
                    return i;
            }
        }
        throw new NotSupportedException("Assembly delimiter could not be found in full type name.");
    }
}
