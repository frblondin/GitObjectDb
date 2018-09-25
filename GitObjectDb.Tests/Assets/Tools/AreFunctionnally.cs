using GitObjectDb.Attributes;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Tests.Assets.Utils
{
    public static class AreFunctionnally
    {
        public static Expression<Func<bool>> Equivalent<TValue>(Expression<Func<bool>> values, params string[] excludedProperties)
        {
            var extracted = ValuesExtractor.Extract(values);
            return Equivalent(typeof(TValue), extracted.A, extracted.B, excludedProperties);
        }

        public static Expression<Func<bool>> Equivalent(Type type, Expression value, Expression expected, params string[] excludedProperties) =>
            Expression.Lambda<Func<bool>>(
                Expression.AndAlso(
                    ReferenceNotEqual(value, expected),
                    PropertiesAreEquivalent(type, value, expected, excludedProperties)));

        static BinaryExpression ReferenceNotEqual(Expression value, Expression expected) =>
            Expression.ReferenceNotEqual(value, expected);

        static Expression PropertiesAreEquivalent(Type type, Expression value, Expression expected, params string[] excludedProperties)
        {
            var result = CompareModifiableProperties(type, value, expected, excludedProperties);
            CompareContainerProperties(type, value, expected, ref result, excludedProperties);

            return result ?? Expression.Constant(true);
        }

        static Expression CompareModifiableProperties(Type type, Expression value, Expression expected, params string[] excludedProperties)
        {
            Expression result = null;
            var modifiableProperties = from p in type.GetTypeInfo().GetProperties()
                                       where !excludedProperties.Contains(p.Name, StringComparer.OrdinalIgnoreCase)
                                       where Attribute.IsDefined(p, typeof(ModifiableAttribute))
                                       select p;
            foreach (var p in modifiableProperties)
            {
                var expression = Expression.Equal(Expression.Property(value, p), Expression.Property(expected, p));
                result = result == null ? expression : Expression.AndAlso(result, expression);
            }

            return result;
        }

        static void CompareContainerProperties(Type type, Expression value, Expression expected, ref Expression result, params string[] excludedProperties)
        {
            var containerProperties = from p in type.GetTypeInfo().GetProperties()
                                      where !excludedProperties.Contains(p.Name, StringComparer.OrdinalIgnoreCase)
                                      where p.PropertyType.GetInterfaces().Prepend(p.PropertyType).Any(t =>
                                            t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IImmutableList<>))
                                      select p;
            foreach (var p in containerProperties)
            {
                var countProperty = p.PropertyType.GetInterfaces().SelectMany(i => i.GetTypeInfo().GetProperties()).First(pr => pr.Name == "Count");
                var valueChildren = Expression.Property(value, p);
                var expectedChildren = Expression.Property(expected, p);
                var sameCount = Expression.Equal(
                    Expression.Property(valueChildren, countProperty),
                    Expression.Property(expectedChildren, countProperty));

                result = result == null ? sameCount : Expression.AndAlso(result, sameCount);
            }
        }

        #region ValuesExtractor
        class ValuesExtractor : ExpressionVisitor
        {
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable SA1306 // Field names must begin with lower-case letter
            Expression A;
            Expression B;
#pragma warning restore SA1306 // Field names must begin with lower-case letter
#pragma warning restore IDE1006 // Naming Styles

            ValuesExtractor()
            {
            }

            public static (Expression A, Expression B) Extract(Expression expression)
            {
                var extractor = new ValuesExtractor();
                extractor.Visit(expression);
                if (extractor.A == null || extractor.B == null)
                {
                    throw new NotSupportedException("Could not extract values from expression.");
                }

                return (extractor.A, extractor.B);
            }

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if (node.NodeType == ExpressionType.Equal)
                {
                    A = node.Left;
                    B = node.Right;
                }
                return base.VisitBinary(node);
            }
        }
        #endregion
    }
}
