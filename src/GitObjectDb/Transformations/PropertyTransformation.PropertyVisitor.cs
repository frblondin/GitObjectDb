using GitObjectDb.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Transformations
{
    internal partial class PropertyTransformation
    {
        [ExcludeFromGuardForNull]
        internal class PropertyVisitor : ExpressionVisitor
        {
            public IDictionary<string, object> Values { get; } = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            internal static PropertyInfo ExtractProperty(Expression node)
            {
                if (node == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }

                var visited = (new PropertyVisitor()).Visit(node);
                var memberExpression = visited as MemberExpression ?? throw new GitObjectDbException("Member expressions expected.");
                return memberExpression.Member as PropertyInfo ?? throw new GitObjectDbException($"The member '{memberExpression.Member.Name}' is not a property.");
            }

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                return base.Visit(node.Body);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                var member = base.VisitMember(node);
                if (node.Expression is MemberExpression)
                {
                    throw new GitObjectDbException("Nested member expressions is not supported.");
                }
                return member;
            }

            public override Expression Visit(Expression node)
            {
                switch (node)
                {
                    case BinaryExpression bin:
                    case MethodCallExpression meth:
                    case ConditionalExpression con:
                    case GotoExpression got:
                    case IndexExpression ind:
                    case InvocationExpression inv:
                    case LabelExpression lab:
                    case LoopExpression loo:
                    case MemberInitExpression memberInit:
                    case NewArrayExpression newArr:
                    case NewExpression newExp:
                    case SwitchExpression switchExpr:
                    case TryExpression tryExp:
                    case UnaryExpression unary:
                    case TypeBinaryExpression typeBinExpr:
                        ThrowNotSupported(node.GetType().Name);
                        break;
                }
                return base.Visit(node);
            }

            private static void ThrowNotSupported(string nodeDetails) =>
                throw new GitObjectDbException($"Expression of type {nodeDetails} is not a valid member access.");
        }
    }
}
