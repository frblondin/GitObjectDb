using GitObjectDb.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Analyzes the conditions of a predicate to collect property assignments to be made.
    /// </summary>
    internal class PredicateReflector
    {
        readonly PredicateVisitor _visitor;

        /// <summary>
        /// Initializes a new instance of the <see cref="PredicateReflector"/> class.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        public PredicateReflector(Expression predicate = null)
        {
            Predicate = predicate;
            _visitor = predicate != null ? CreateVisitor(predicate) : null;
        }

        /// <summary>
        /// Gets an empty predicate.
        /// </summary>
        public static PredicateReflector Empty { get; } = new PredicateReflector();

        /// <summary>
        /// Gets the process argument method.
        /// </summary>
        internal static MethodInfo ProcessArgumentMethod { get; } = typeof(PredicateReflector).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single(m => m.Name == nameof(ProcessArgument));

        /// <summary>
        /// Gets the predicate.
        /// </summary>
        public Expression Predicate { get; }

        static PredicateVisitor CreateVisitor(Expression predicate)
        {
            var visitor = new PredicateVisitor();
            visitor.Visit(predicate);
            return visitor;
        }

        /// <summary>
        /// Returns the value collected by the reflector, <paramref name="fallback"/> is none was found.
        /// </summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="fallback">The fallback value.</param>
        /// <returns>The final value to be used for the parameter.</returns>
        public TValue ProcessArgument<TValue>(string name, TValue fallback) =>
            _visitor != null && _visitor.Values.TryGetValue(name, out var value) ?
            (TValue)value :
            fallback;

        #region PredicateVisitor
        class PredicateVisitor : ExpressionVisitor
        {
            /// <summary>
            /// Gets the values.
            /// </summary>
            public IDictionary<string, object> Values { get; } = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            static MemberInfo ExtractMember(Expression node)
            {
                var memberExpression = node as MemberExpression ?? throw new NotSupportedException("Member expressions expected in predicate.");
                if (!Attribute.IsDefined(memberExpression.Member, typeof(ModifiableAttribute)))
                {
                    throw new NotSupportedException($"Member expressions should be decorated with {nameof(ModifiableAttribute)} attribute.");
                }

                return memberExpression.Member;
            }

            static object ExtractValue(Expression node)
            {
                switch (node)
                {
                    case ConstantExpression constant:
                        return constant.Value;
                    case DefaultExpression @default:
                        return null;
                    case MemberExpression member:
                        return GetMemberValue(
                            ExtractValue(member.Expression),
                            member.Member);
                    default: throw new NotSupportedException("Only constant expressions expected in predicate right-hand values.");
                }
            }

            static object GetMemberValue(object instance, MemberInfo member)
            {
                switch (member)
                {
                    case FieldInfo field: return field.GetValue(instance);
                    case PropertyInfo property: return property.GetValue(instance);
                    default: throw new NotSupportedException($"Unsupported member type {member.GetType()}.");
                }
            }

            static void ThrowNotSupported(string nodeDetails) =>
                throw new NotSupportedException($"Expression of type {nodeDetails} is not suppoerted in predicate reflector.");

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.TypeEqual)
                {
                    var member = ExtractMember(node.Left);
                    var value = ExtractValue(node.Right);

                    Values[member.Name] = value;
                }
                else
                {
                    ThrowNotSupported(node.NodeType.ToString());
                }
                return base.VisitBinary(node);
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
                    case MethodCallExpression methodCall:
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
        #endregion
    }
}
