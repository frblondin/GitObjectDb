using GitObjectDb.Attributes;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Extracts reflection information from provided expressions.
    /// </summary>
    internal static class ExpressionReflector
    {
        /// <summary>
        /// Extracts the <see cref="MethodInfo"/> out of the expression.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <param name="returnGenericDefinition">if set to <c>true</c> returns the generic method definition.</param>
        /// <returns>The <see cref="MethodInfo"/>.</returns>
        internal static MethodInfo GetMethod<TSource>(Expression<Action<TSource>> expression, bool returnGenericDefinition = false)
        {
            var found = Visitor<MethodCallExpression>.Lookup(expression);
            return returnGenericDefinition ? found.Method.GetGenericMethodDefinition() : found.Method;
        }

        /// <summary>
        /// Extracts the <see cref="ConstructorInfo"/> out of the expression.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>The <see cref="ConstructorInfo"/>.</returns>
        internal static ConstructorInfo GetConstructor<TSource>(Expression<Func<TSource>> expression)
        {
            var found = Visitor<NewExpression>.Lookup(expression);
            return found.Constructor;
        }

        /// <summary>
        /// Extracts the <see cref="PropertyInfo"/> out of the expression.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>The <see cref="PropertyInfo"/>.</returns>
        internal static PropertyInfo GetProperty<TSource>(Expression<Func<TSource, object>> expression)
        {
            var found = Visitor<MemberExpression>.Lookup(expression);
            return (PropertyInfo)found.Member;
        }

        [ExcludeFromGuardForNull]
        class Visitor<TExpression> : ExpressionVisitor
            where TExpression : Expression
        {
            public TExpression Result { get; private set; }

            internal static TExpression Lookup(Expression expression)
            {
                var visitor = new Visitor<TExpression>();
                visitor.Visit(expression);
                return visitor.Result ??
                    throw new NotSupportedException($"Unable to find an expression of type {typeof(TExpression).Name} in expression.");
            }

            public override Expression Visit(Expression node)
            {
                if (node == null)
                {
                    return null;
                }

                if (node is TExpression found)
                {
                    if (Result != null)
                    {
                        throw new NotSupportedException($"Only one expression of type {nameof(TExpression)} expected.");
                    }
                    Result = found;
                }
                return base.Visit(node);
            }
        }
    }
}
