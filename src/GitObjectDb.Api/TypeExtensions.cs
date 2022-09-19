using System.Reflection;

namespace GitObjectDb.Api;

/// <summary>Provides extension methods for <see cref="Type"/> type.</summary>
public static class TypeExtensions
{
    /// <summary>
    /// Gets whether the property is an enumerable whose type matches given <paramref name="predicate"/>.
    /// </summary>
    /// <param name="property">The property to be analyzed.</param>
    /// <param name="predicate">The predicate to be applied on generic arguments.</param>
    /// <param name="type">The generic argument type marching the <paramref name="predicate"/>.</param>
    /// <returns><c>true</c> if a matching enumerable could be found, <c>false</c> otherwise.</returns>
    public static bool IsEnumerable(this PropertyInfo property, Predicate<Type> predicate, out Type? type)
    {
        var arg = FindInterfaceDefinitionArgument(property.PropertyType, typeof(IEnumerable<>));
        if (arg is not null && predicate(arg))
        {
            type = arg;
            return true;
        }
        else
        {
            type = null;
            return false;
        }
    }

    private static Type? FindInterfaceDefinitionArgument(Type type, Type interfaceDefinition)
    {
        if (IsInterfaceDefinition(type))
        {
            return type.GetGenericArguments()[0];
        }
        var @interface = type.GetInterfaces().FirstOrDefault(IsInterfaceDefinition);
        return @interface?.GetGenericArguments()[0];

        bool IsInterfaceDefinition(Type nestedType) =>
            nestedType.IsInterface &&
            nestedType.IsGenericType &&
            nestedType.GetGenericTypeDefinition() == interfaceDefinition;
    }
}
