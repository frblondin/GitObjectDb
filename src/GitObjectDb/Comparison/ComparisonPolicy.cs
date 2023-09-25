using GitObjectDb.Model;
using GitObjectDb.Tools;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace GitObjectDb.Comparison;

/// <summary>Provides the description of a merge policy.</summary>
public record ComparisonPolicy
{
    /// <summary>Gets ignored node properties.</summary>
    public IImmutableList<PropertyInfo> IgnoredProperties { get; init; } = ImmutableList.Create<PropertyInfo>();

    /// <summary>Gets ignored class, property or field when decorated with attributes.</summary>
    public IImmutableList<Type> AttributesToIgnore { get; init; } = ImmutableList.Create<Type>();

    /// <summary>
    /// Gets a value indicating whether If <c>true</c>, <see cref="string.Empty"/> and <c>null</c> will be treated as
    /// equal for <see cref="string"/> and <see cref="System.Text.StringBuilder"/>. The default is <c>false</c>.
    /// </summary>
    public bool TreatStringEmptyAndNullTheSame { get; init; }

    /// <summary>
    /// Gets a value indicating whether leading and trailing whitespaces will be ignored for
    /// for <see cref="string"/> and <see cref="System.Text.StringBuilder"/>. The default is <c>false</c>.
    /// </summary>
    public bool IgnoreStringLeadingTrailingWhitespace { get; init; }

    /// <summary>Gets a configuration callback that will be invoked while initializing the comparer.</summary>
    public Action<ObjectsComparer.Comparer>? Configure { get; init; }

    /// <summary>
    /// Creates the default policy for a given model, ignoring properties decorated
    /// with <see cref="IgnoreDataMemberAttribute"/>.
    /// </summary>
    /// <param name="model">The data model to extract properties from.</param>
    /// <returns>The comparison policy.</returns>
    public static ComparisonPolicy CreateDefault(IDataModel model)
    {
        return new()
        {
            IgnoredProperties = ImmutableList<PropertyInfo>.Empty
                .AddRange(model, IgnoreNonSerializedProperty),
        };

        static bool IgnoreNonSerializedProperty(NodeTypeDescription typeDescription, PropertyInfo property) =>
            !typeDescription.SerializableProperties.Contains(property) &&
            !(property.DeclaringType == typeof(Node) && property.Name == nameof(Node.EmbeddedResource)) &&
            !(property.DeclaringType == typeof(TreeItem) && property.Name == nameof(TreeItem.Path));
    }
}

/// <summary>Adds ability to add ignored properties.</summary>
public static class ComparisonPolicyExtensions
{
    /// <summary>
    /// Makes a copy of the list, and adds the specified property to the end of the copied.
    /// </summary>
    /// <typeparam name="TNode">The type of the node whose property must be added to the list.</typeparam>
    /// <param name="source">The list.</param>
    /// <param name="propertyExpression">The expression describing the property to add to the list.</param>
    /// <returns>A new list with the property added.</returns>
    public static IImmutableList<PropertyInfo> Add<TNode>(this IImmutableList<PropertyInfo> source,
                                                          Expression<Func<TNode, object?>> propertyExpression)
        where TNode : Node
    {
        var property = ExpressionReflector.GetProperty(propertyExpression);
        return source.Add(property);
    }

    /// <summary>
    /// Makes a copy of the list, and adds the properties that are approved
    /// by <paramref name="propertyPredicate"/>.
    /// </summary>
    /// <param name="source">The list.</param>
    /// <param name="dataModel">The data model to extract properties from.</param>
    /// <param name="propertyPredicate">The method that returns true if a <see cref="PropertyInfo"/>
    /// should be added to the list.</param>
    /// <returns>A new list with the property added.</returns>
    public static IImmutableList<PropertyInfo> AddRange(this IImmutableList<PropertyInfo> source,
                                                        IDataModel dataModel,
                                                        Func<NodeTypeDescription, PropertyInfo, bool> propertyPredicate)
    {
        var builder = ImmutableList.CreateBuilder<PropertyInfo>();
        builder.AddRange(source);
        foreach (var type in dataModel.NodeTypes)
        {
            foreach (var property in from property in type.Type.GetProperties()
                                     where propertyPredicate(type, property)
                                     select property)
            {
                builder.Add(property);
            }
        }
        return builder.ToImmutable();
    }
}
