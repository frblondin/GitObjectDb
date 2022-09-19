using System.Reflection;

namespace GitObjectDb.Api.Model;

/// <summary>
/// Provides a description of data transfer objects (original <see cref="Node"/> type...).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DtoDescriptionAttribute : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="DtoDescriptionAttribute"/> class.</summary>
    /// <param name="type">The <see cref="Node"/> type.</param>
    /// <param name="entitySetName">The entity set name.</param>
    public DtoDescriptionAttribute(Type type, string entitySetName)
    {
        Type = type;
        EntitySetName = entitySetName;
    }

    /// <summary>Gets the <see cref="Node"/> type.</summary>
    public Type Type { get; }

    /// <summary>Gets the entity set name.</summary>
    public string EntitySetName { get; }

    /// <summary>
    /// Gets the <see cref="DtoDescriptionAttribute"/> attribute from given <see cref="NodeDto"/> type.
    /// </summary>
    /// <param name="type">The <see cref="NodeDto"/> type.</param>
    /// <returns>The <see cref="DtoDescriptionAttribute"/> instance.</returns>
    /// <exception cref="NotSupportedException">No attribute defined.</exception>
    public static DtoDescriptionAttribute Get(Type type) =>
        type.GetCustomAttribute<DtoDescriptionAttribute>() ??
        throw new NotSupportedException(
            $"No {nameof(DtoDescriptionAttribute)} attribute defined for type '{type}'.");
}
