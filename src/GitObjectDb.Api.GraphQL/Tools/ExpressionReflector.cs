using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Tools;

/// <summary>Extracts reflection information from provided expressions.</summary>
internal static class ExpressionReflector
{
    /// <summary>Extracts the <see cref="MethodInfo"/> out of the expression.</summary>
    /// <param name="expression">The expression.</param>
    /// <param name="returnGenericDefinition">if set to <c>true</c> returns the generic method definition.</param>
    /// <returns>The <see cref="MethodInfo"/>.</returns>
    [ExcludeFromCodeCoverage]
    internal static MethodInfo GetMethod(Expression<Action> expression, bool returnGenericDefinition = false)
    {
        var found = Visitor<MethodCallExpression>.Lookup(expression);
        return returnGenericDefinition ? found.Method.GetGenericMethodDefinition() : found.Method;
    }

    /// <summary>Extracts the <see cref="PropertyInfo"/> out of the expression.</summary>
    /// <typeparam name="TSource">The type of the source.</typeparam>
    /// <param name="expression">The expression.</param>
    /// <returns>The <see cref="PropertyInfo"/>.</returns>
    [ExcludeFromCodeCoverage]
    internal static PropertyInfo GetProperty<TSource>(Expression<Func<TSource, object?>> expression)
    {
        var found = Visitor<MemberExpression>.Lookup(expression);
        return (PropertyInfo)found.Member;
    }

    private class Visitor<TExpression> : ExpressionVisitor
        where TExpression : Expression
    {
        public TExpression? Result { get; private set; }

        internal static TExpression Lookup(Expression expression)
        {
            var visitor = new Visitor<TExpression>();
            visitor.Visit(expression);
            return visitor.Result ??
                throw new GitObjectDbException($"Unable to find an expression of type {typeof(TExpression).Name} in expression.");
        }

        public override Expression? Visit(Expression? node)
        {
            switch (node)
            {
                case null:
                    return null;
                case TExpression found:
                    ThrowIfExpressionAlreadyFound();
                    Result = found;
                    return base.Visit(node);
                default:
                    return base.Visit(node);
            }
        }

        [ExcludeFromCodeCoverage]
        private void ThrowIfExpressionAlreadyFound()
        {
            if (Result != null)
            {
                throw new GitObjectDbException($"Only one expression of type {nameof(TExpression)} expected.");
            }
        }
    }
}
