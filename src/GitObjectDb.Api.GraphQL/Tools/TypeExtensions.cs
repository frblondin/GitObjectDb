namespace GitObjectDb.Api.GraphQL.Tools;

/// <summary>Provides extension methods for <see cref="Type"/> type.</summary>
internal static class TypeExtensions
{
    /// <summary>Gets whether the type is a <see cref="Node"/> reference.</summary>
    /// <param name="type">The type to be analyzed.</param>
    /// <returns><c>true</c> if the type is a Node reference, <c>false</c> otherwise.</returns>
    internal static bool IsNode(this Type type) =>
        type.IsAssignableTo(typeof(Node));

    /// <summary>Gets whether the type is a <see cref="Node"/> enumerable reference.</summary>
    /// <param name="type">The type to be analyzed.</param>
    /// <param name="argumentType">The node type that has been found, if any.</param>
    /// <returns><c>true</c> if the type is a Node enumerable reference, <c>false</c> otherwise.</returns>
    internal static bool IsNodeEnumerable(this Type type, out Type? argumentType) =>
        type.IsEnumerable(t => t.IsAssignableTo(typeof(Node)), out argumentType);

    /// <summary>
    /// Gets whether the type is an enumerable whose type matches given <paramref name="predicate"/>.
    /// </summary>
    /// <param name="type">The type to be analyzed.</param>
    /// <param name="predicate">The predicate to be applied on generic arguments.</param>
    /// <param name="argumentType">The generic argument type matching the <paramref name="predicate"/>.</param>
    /// <returns><c>true</c> if a matching enumerable could be found, <c>false</c> otherwise.</returns>
    internal static bool IsEnumerable(this Type type, Predicate<Type> predicate, out Type? argumentType)
    {
        var arg = FindInterfaceDefinitionArgument(type, typeof(IEnumerable<>));
        if (arg is not null && predicate(arg))
        {
            argumentType = arg;
            return true;
        }
        else
        {
            argumentType = null;
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
