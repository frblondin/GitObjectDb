using System.Reflection;

namespace GitObjectDb.Api.Model;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DtoDescriptionAttribute : Attribute
{
    public DtoDescriptionAttribute(Type type, string entitySetName)
    {
        Type = type;
        EntitySetName = entitySetName;
    }

    public Type Type { get; }

    public string EntitySetName { get; }

    public static DtoDescriptionAttribute Get(Type type) =>
        type.GetCustomAttribute<DtoDescriptionAttribute>() ??
        throw new NotSupportedException(
            $"No {nameof(DtoDescriptionAttribute)} attribute defined for type '{type}'.");
}
