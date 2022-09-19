using GitObjectDb.Api.Model;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Api;

public static class TypeExtensions
{
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
