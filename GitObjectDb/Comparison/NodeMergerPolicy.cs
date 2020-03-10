using GitObjectDb.Tools;
using System;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Comparison
{
    /// <summary>Provides the description of a node merge policy.</summary>
    public class NodeMergerPolicy
    {
        internal NodeMergerPolicy()
            : this(ImmutableList.Create<PropertyInfo>())
        {
        }

        private NodeMergerPolicy(IImmutableList<PropertyInfo> ignoredProperties)
        {
            IgnoredProperties = ignoredProperties;
        }

        /// <summary>Gets the default policy.</summary>
        public static NodeMergerPolicy Default { get; } = new NodeMergerPolicy()
            .IgnoreProperty((Node n) => n.Path);

        internal IImmutableList<PropertyInfo> IgnoredProperties { get; }

        /// <summary>Ignores a node property to the policy.</summary>
        /// <typeparam name="TNode">The type of the node.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>The current <see cref="NodeMergerPolicy"/> instance.</returns>
        public NodeMergerPolicy IgnoreProperty<TNode>(Expression<Func<TNode, object>> expression)
            where TNode : Node
        {
            var property = ExpressionReflector.GetProperty(expression);
            return new NodeMergerPolicy(IgnoredProperties.Add(property));
        }
    }
}
