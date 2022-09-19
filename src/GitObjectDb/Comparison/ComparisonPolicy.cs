using GitObjectDb.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace GitObjectDb.Comparison
{
    /// <summary>Provides the description of a merge policy.</summary>
    public record ComparisonPolicy
    {
        /// <summary>Gets the default policy.</summary>
        public static ComparisonPolicy Default { get; } = new ComparisonPolicy().UpdateWithDefaultExclusion();

        /// <summary>Gets ignored node properties.</summary>
        public IImmutableList<PropertyInfo> IgnoredProperties { get; init; } = ImmutableList.Create<PropertyInfo>();

        /// <summary>Gets ignored class, property or field when decorated with attributes.</summary>
        public IImmutableList<Type> AttributesToIgnore { get; init; } = ImmutableList.Create<Type>();
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
                AttributesToIgnore = source.AttributesToIgnore.Add(typeof(JsonIgnoreAttribute)),
            };
        }

        /// <summary>Makes a copy of the list, and adds the specified property to the end of the copied.</summary>
        /// <typeparam name="TNode">The type of the node whose property must be added to the list.</typeparam>
        /// <param name="source">The list.</param>
        /// <param name="expression">The expression describing the property to add to the list.</param>
        /// <returns>A new list with the property added.</returns>
        public static IImmutableList<PropertyInfo> Add<TNode>(this IImmutableList<PropertyInfo> source, Expression<Func<TNode, object?>> expression)
            where TNode : Node
        {
            var property = ExpressionReflector.GetProperty(expression);
            return source.Add(property);
        }
    }
}
