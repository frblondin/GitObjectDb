using GitObjectDb.Attributes;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Reflection
{
    internal partial class PredicateReflector
    {
        /// <summary>
        /// Visitor used to extract the concrete value out of an expression.
        /// </summary>
        /// <seealso cref="ExpressionVisitor" />
        [ExcludeFromGuardForNull]
        internal class ValueVisitor : ExpressionVisitor
        {
            private ValueVisitor()
            {
            }

            /// <summary>
            /// Extracts the value.
            /// </summary>
            /// <param name="node">The node.</param>
            /// <returns>The extracted value.</returns>
            public static object ExtractValue(Expression node) => new ValueVisitor().ExtractValueImpl(node);

            object ExtractValueImpl(Expression node)
            {
                var expression = Visit(node);
                switch (expression)
                {
                    case null:
                        return null;
                    case ConstantExpression constant:
                        return constant.Value;
                    case DefaultExpression @default:
                        return @default.Type.IsByRef ? null : Activator.CreateInstance(@default.Type);
                    default:
                        throw new NotSupportedException("Could node extract a constant value out of the expression.");
                }
            }

            /// <inheritdoc/>
            protected override Expression VisitMember(MemberExpression node)
            {
                switch (node.Member)
                {
                    case FieldInfo field:
                        return Expression.Constant(field.GetValue(ExtractValueImpl(node.Expression)), node.Type);
                    case PropertyInfo property:
                        return Expression.Constant(property.GetValue(ExtractValueImpl(node.Expression)), node.Type);
                    default:
                        throw new NotSupportedException($"Unsupported member type {node.Member.GetType()}.");
                }
            }

            /// <inheritdoc/>
            protected override Expression VisitIndex(IndexExpression node)
            {
                var instance = ExtractValueImpl(node.Object);
                var arguments = node.Arguments.Select(a => ExtractValueImpl(a)).ToArray();
                return Expression.Constant(node.Indexer.GetValue(instance, arguments), node.Type);
            }

            /// <inheritdoc/>
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var instance = ExtractValueImpl(node.Object);
                var arguments = node.Arguments.Select(a => ExtractValueImpl(a)).ToArray();
                return Expression.Constant(node.Method.Invoke(instance, arguments), node.Type);
            }

            /// <inheritdoc/>
            protected override Expression VisitNew(NewExpression node)
            {
                var arguments = node.Arguments.Select(a => ExtractValueImpl(a)).ToArray();
                return Expression.Constant(node.Constructor.Invoke(arguments));
            }
        }
    }
}
