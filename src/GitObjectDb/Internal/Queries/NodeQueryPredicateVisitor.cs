using System;
using System.IO;
using System.Linq.Expressions;

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

        internal bool Incompatible { get; private set; }

        public override Expression Visit(Expression node)
        {
            if (Incompatible)
            {
                return node;
            }

            // We only support a subset of expression types
            switch (node)
            {
                case BinaryExpression _:
                case ParameterExpression _:
                case MemberExpression _:
                case ConstantExpression _:
                    return base.Visit(node);
                default:
                    Incompatible = true;
                    return node;
            }
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression == _oldParameter)
            {
                switch (node.Member.Name)
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
            return base.VisitMember(node);
        }
    }
}
