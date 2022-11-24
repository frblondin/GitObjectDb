using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.YamlDotNet;

/// <summary>
/// type extensions.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Converts the compiled .Net type to its corresponding Nabsic name.
    /// </summary>
    /// <param name="type">the type.</param>
    /// <returns>type name.</returns>
    /// <exception cref="ArgumentNullException">type is null.</exception>
    public static string GetYamlName(this Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        if (type.AssemblyQualifiedName != null &&
            type.AssemblyQualifiedName.Contains("DynamicProxy") &&
            type.BaseType != null)
        {
           return type.BaseType.GetYamlName();
        }

        var nonNullableType = Nullable.GetUnderlyingType(type);

        if (nonNullableType != null)
        {
            return nonNullableType.GetYamlName() + "?";
        }
        return type.GetSubTypeGeneric()
                   .Replace('+', '.');
    }

    private static string GetSubTypeGeneric(this Type type)
    {
        if (type.IsGenericType)
        {
            var typeDefinition = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();
            string result = type.GetShortName();
            if (typeDefinition != type)
            {
                result += "(" + string.Join(",", type.GetGenericArguments().Select(argType => GetYamlName(argType))) + ")";
            }
            else
            {
                result += "()";
            }
            return result;
        }
        else if (type.IsArray)
        {
            return string.Format("Array({0})",
                type.GetElementType().GetYamlName());
        }
        return type.GetShortName();
    }

    /// <summary>
    /// get the short name of the type.
    /// </summary>
    /// <param name="type">the type.</param>
    /// <returns>short name of the type.</returns>
    private static string GetShortName(this Type type)
    {
        if (type == typeof(int))
        {
            return type.Name.ToLowerInvariant().Replace("32", string.Empty);
        }
        if (type == typeof(object) || type == typeof(string) || type.IsPrimitive)
        {
            return type.Name.ToLowerInvariant();
        }
        if (type.IsGenericType)
        {
            return type.FullName.Substring(0, type.FullName.IndexOf("`"));
        }
        return type.FullName;
    }
}
