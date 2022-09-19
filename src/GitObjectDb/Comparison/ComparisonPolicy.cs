using GitObjectDb.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Comparison
{
    /// <summary>Provides the description of a merge policy.</summary>
    public class ComparisonPolicy
    {
        internal ComparisonPolicy()
            : this(ImmutableList.Create<PropertyInfo>())
        {
        }

        internal ComparisonPolicy(IImmutableList<PropertyInfo> ignoredProperties)
        {
            IgnoredProperties = ignoredProperties;
        }

        /// <summary>Gets the default policy.</summary>
        public static ComparisonPolicy Default { get; } = new ComparisonPolicy()
            .UpdateWithDefaultExclusion();

        internal IImmutableList<PropertyInfo> IgnoredProperties { get; }
    }

#pragma warning disable SA1402 // File may only contain a single type
    /// <summary>Adds ability to add ignored properties.</summary>
    public static class ComparisonPolicyExtensions
    {
        /// <summary>Ignores a node property to the policy.</summary>
        /// <typeparam name="TPolicy">The type of the policy.</typeparam>
        /// <typeparam name="TNode">The type of the node.</typeparam>
        /// <param name="source">The policy to be updated.</param>
        /// <param name="expression">The expression.</param>
        /// <returns>The current <see cref="ComparisonPolicy"/> instance.</returns>
        public static TPolicy IgnoreProperty<TPolicy, TNode>(this TPolicy source, Expression<Func<TNode, object?>> expression)
            where TPolicy : ComparisonPolicy
            where TNode : Node
        {
            var property = ExpressionReflector.GetProperty(expression);
            return (TPolicy)Fasterflect.Reflect.Constructor(typeof(TPolicy), typeof(IImmutableList<PropertyInfo>))
                .Invoke(source.IgnoredProperties.Add(property));
        }

        /// <summary>Ignores a node property to the policy.</summary>
        /// <typeparam name="TPolicy">The type of the policy.</typeparam>
        /// <param name="source">The policy to be updated.</param>
        /// <param name="properties">The properties to ignore.</param>
        /// <returns>The current <see cref="ComparisonPolicy"/> instance.</returns>
        public static TPolicy IgnoreProperties<TPolicy>(this TPolicy source, IEnumerable<PropertyInfo> properties)
            where TPolicy : ComparisonPolicy
        {
            return (TPolicy)Fasterflect.Reflect.Constructor(typeof(TPolicy), typeof(IImmutableList<PropertyInfo>))
                .Invoke(source.IgnoredProperties.AddRange(properties));
        }

        internal static TPolicy UpdateWithDefaultExclusion<TPolicy>(this TPolicy source)
            where TPolicy : ComparisonPolicy
        {
            return source.IgnoreProperty((Node n) => n.Path);
        }
    }
}
