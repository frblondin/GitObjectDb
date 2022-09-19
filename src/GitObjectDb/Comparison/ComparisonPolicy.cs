using GitObjectDb.Model;
using GitObjectDb.Tools;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace GitObjectDb.Comparison;

/// <summary>Provides the description of a merge policy.</summary>
public record ComparisonPolicy
{
    /// <summary>Gets ignored node properties.</summary>
    public IImmutableList<PropertyInfo> IgnoredProperties { get; init; } = ImmutableList.Create<PropertyInfo>();

    /// <summary>Gets ignored class, property or field when decorated with attributes.</summary>
    public IImmutableList<Type> AttributesToIgnore { get; init; } = ImmutableList.Create<Type>();

    /// <summary>Gets a list of custom comparers that take priority over the built in comparers.</summary>
    public IImmutableList<BaseTypeComparer> CustomComparers { get; init; } = ImmutableList.Create<BaseTypeComparer>();

    /// <summary>
    /// Creates the default policy for a given model, ignoring properties decorated
    /// with <see cref="JsonIgnoreAttribute"/>.
    /// </summary>
    /// <param name="model">The data model to extract properties from.</param>
    /// <returns>The comparison policy.</returns>
    public static ComparisonPolicy CreateDefault(IDataModel model)
    {
        var @default = new ComparisonPolicy().UpdateWithDefaultExclusion();
        return @default with { IgnoredProperties = @default.IgnoredProperties.AddRange(model, IgnoreProperty) };

        bool IgnoreProperty(PropertyInfo property)
        {
            var isJsonIgnored = property.GetCustomAttribute<JsonIgnoreAttribute>() is not null;
            return isJsonIgnored && property.Name != nameof(Node.EmbeddedResource);
        }
    }
}

#pragma warning disable SA1402 // File may only contain a single type
/// <summary>Adds ability to add ignored properties.</summary>
public static class ComparisonPolicyExtensions
{
    internal static ComparisonPolicy UpdateWithDefaultExclusion(this ComparisonPolicy source)
        {
        return source with
        {
            IgnoredProperties = source.IgnoredProperties.Add((Node n) => n.Path),
        };
    }

    /// <summary>Adds custom comparers that take priority over the built in comparers.</summary>
    /// <param name="source">The comparison policy to update.</param>
    /// <param name="comparers">The custom comparers to add.</param>
    /// <returns>A new comparison policy with added comparer.</returns>
    public static ComparisonPolicy UpdateWithCustomComparer(this ComparisonPolicy source, params BaseTypeComparer[] comparers)
    {
        return source with
        {
            CustomComparers = source.CustomComparers.AddRange(comparers),
        };
    }

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
                                                        Predicate<PropertyInfo> propertyPredicate)
    {
        var builder = ImmutableList.CreateBuilder<PropertyInfo>();
        builder.AddRange(source);
        foreach (var type in dataModel.NodeTypes)
        {
            foreach (var property in type.Type.GetProperties().Except(builder))
            {
                if (propertyPredicate(property))
                {
                    builder.Add(property);
                }
            }
        }
        return builder.ToImmutable();
    }
}
