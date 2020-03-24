using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Internal.Queries
{
    internal class NodeQueryPredicateVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;

        internal NodeQueryPredicateVisitor(ParameterExpression oldParameter)
        {
            _oldParameter = oldParameter;
        }

        internal static ParameterExpression NewParameter { get; } = Expression.Parameter(typeof((DataPath, Func<Stream>)));

        private static Expression Path => Expression.Field(NewParameter, "Item1");

        private static Expression Stream => Expression.Call(
            Expression.Field(NewParameter, "Item2"), "Invoke", null);

        internal bool Incompatible { get; private set; }

        public override Expression Visit(Expression expression)
        {
            // We only support a subset of expression types
            switch (expression)
            {
                case BinaryExpression _:
                case ParameterExpression _:
                case MemberExpression _:
                case ConstantExpression _:
                    if (!Incompatible)
                    {
                        return base.Visit(expression);
                    }
                    goto default;
                default:
                    Incompatible = true;
                    return expression;
            }
        }

        protected override Expression VisitMember(MemberExpression member)
        {
            if (member.Expression == _oldParameter)
            {
                switch (member.Member.Name)
                {
                    case nameof(Node.Id):
                        var folderName = Expression.Property(Path, nameof(DataPath.FolderName));
                        return Expression.New(UniqueId.Constructor, folderName);
                    case nameof(Node.Path):
                        return Path;
                    default:
                        Incompatible = true;
                        break;
                }
            }
            return base.VisitMember(member);
        }
    }
}
