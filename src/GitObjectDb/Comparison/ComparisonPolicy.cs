using GitObjectDb.Tools;
using System;
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

        private ComparisonPolicy(IImmutableList<PropertyInfo> ignoredProperties)
        {
            IgnoredProperties = ignoredProperties;
        }

        /// <summary>Gets the default policy.</summary>
        public static ComparisonPolicy Default { get; } = new ComparisonPolicy()
            .IgnoreProperty<Node>(n => n.Path);

        internal IImmutableList<PropertyInfo> IgnoredProperties { get; }

        /// <summary>Ignores a node property to the policy.</summary>
        /// <typeparam name="TNode">The type of the node.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>The current <see cref="ComparisonPolicy"/> instance.</returns>
        public ComparisonPolicy IgnoreProperty<TNode>(Expression<Func<TNode, object?>> expression)
            where TNode : Node
        {
            var property = ExpressionReflector.GetProperty(expression);
            return new ComparisonPolicy(IgnoredProperties.Add(property));
        }
    }
}
