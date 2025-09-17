using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GitObjectDb.YamlDotNet;

/// <summary>Type extensions.</summary>
[ExcludeFromCodeCoverage]
public static class TypeExtensions
{
    /// <summary>Gets the YAML name of the specified type.</summary>
    /// <param name="type">The type to get the YAML name for.</param>
    /// <returns>The YAML name of the type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the type is null.</exception>
    public static string GetYamlName(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

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
                result += $"({string.Join(",", type.GetGenericArguments().Select(GetYamlName))})";
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
                type.GetElementType()!.GetYamlName());
        }
        return type.GetShortName();
    }

    /// <summary>Gets the short name of the type.</summary>
    /// <param name="type">The type.</param>
    /// <returns>The short name of the type.</returns>
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
            return type.FullName!.Substring(0, type.FullName.IndexOf("`"));
        }
        return type.FullName!;
    }
}