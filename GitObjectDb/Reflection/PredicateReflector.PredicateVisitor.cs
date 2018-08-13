using GitObjectDb.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Reflection
{
    internal partial class PredicateReflector
    {
        [ExcludeFromGuardForNull]
        class PredicateVisitor : ExpressionVisitor
        {
            public IDictionary<string, object> Values { get; } = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            public IDictionary<string, IList<ChildChange>> ChildChanges { get; } = new SortedDictionary<string, IList<ChildChange>>(StringComparer.OrdinalIgnoreCase);

            static MemberInfo ExtractMember(Expression node)
            {
                var memberExpression = node as MemberExpression ?? throw new NotSupportedException("Member expressions expected in predicate.");
                if (!Attribute.IsDefined(memberExpression.Member, typeof(ModifiableAttribute)))
                {
                    throw new NotSupportedException($"Member expressions should be decorated with {nameof(ModifiableAttribute)} attribute.");
                }

                return memberExpression.Member;
            }

            static void ThrowNotSupported(string nodeDetails) =>
                throw new NotSupportedException($"Expression of type {nodeDetails} is not supported in predicate reflector.");

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if (node == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }

                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.TypeEqual:
                        var member = ExtractMember(node.Left);
                        Values[member.Name] = ValueVisitor.ExtractValue(node.Right);
                        return node;
                    case ExpressionType.And:
                    case ExpressionType.AndAlso:
                        return base.VisitBinary(node);
                }
                ThrowNotSupported(node.NodeType.ToString());
                return node;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }

                if (node.Method == _childrenAddMethod)
                {
                    VisitAddOrDeleteMethodCall(node, ChildChangeType.Add);
                    return node;
                }
                else if (node.Method == _childrenDeleteMethod)
                {
                    VisitAddOrDeleteMethodCall(node, ChildChangeType.Delete);
                    return node;
                }
                else
                {
                    ThrowNotSupported(node.NodeType.ToString());
                }
                return base.VisitMethodCall(node);
            }

            void VisitAddOrDeleteMethodCall(MethodCallExpression node, ChildChangeType changeType)
            {
                var instance = node.Object as MemberExpression ??
                    throw new NotSupportedException($"{changeType.ToString()} method is only supported when called on an object child property, eg. object.ChildProperty.{changeType.ToString()}(...).");
                var value = ValueVisitor.ExtractValue(node.Arguments[0]) as IMetadataObject ??
                    throw new NotSupportedException("The parameter provided for the Add method call could not be extracted as a non-nullable instance.");

                var changes = GetChildChangeList(instance.Member.Name);
                changes.Add(new ChildChange(value, changeType));
            }

            IList<ChildChange> GetChildChangeList(string memberName)
            {
                if (!ChildChanges.TryGetValue(memberName, out var result))
                {
                    ChildChanges[memberName] = result = new List<ChildChange>();
                }
                return result;
            }

            public override Expression Visit(Expression node)
            {
                switch (node)
                {
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
                    case TypeBinaryExpression typeBinExpr:
                        ThrowNotSupported(node.GetType().Name);
                        break;
                }
                return base.Visit(node);
            }
        }
    }
}
