using GitObjectDb.Attributes;
using GitObjectDb.Models;
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
    internal partial class PredicateReflector
    {
        static readonly MethodInfo _childrenAddMethod = ExpressionReflector.GetMethod<ILazyChildren>(c => c.Add(default));
        static readonly MethodInfo _childrenDeleteMethod = ExpressionReflector.GetMethod<ILazyChildren>(c => c.Delete(default));

        readonly PredicateVisitor _visitor;

        private PredicateReflector()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PredicateReflector"/> class.
        /// </summary>
        /// <param name="predicate">The predicate.</param>
        public PredicateReflector(Expression predicate)
        {
            Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _visitor = predicate != null ? CreateVisitor(predicate) : null;
        }

        /// <summary>
        /// Gets an empty predicate.
        /// </summary>
        public static PredicateReflector Empty { get; } = new PredicateReflector();

        /// <summary>
        /// Gets the process argument method.
        /// </summary>
        internal static MethodInfo ProcessArgumentMethod { get; } =
            ExpressionReflector.GetMethod<PredicateReflector>(r => r.ProcessArgument<string>(default, default), returnGenericDefinition: true);

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
        internal TValue ProcessArgument<TValue>(string name, TValue fallback) =>
            _visitor != null && _visitor.Values.TryGetValue(name, out var value) ?
            (TValue)value :
            fallback;

        /// <summary>
        /// Gets the child changes collected by the reflector.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A list of changes.</returns>
        internal IList<ChildChange> TryGetChildChanges(string name) =>
            _visitor != null && _visitor.ChildChanges.TryGetValue(name, out var changes) ?
            changes :
            null;

        #region PredicateVisitor
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

                if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.TypeEqual)
                {
                    var member = ExtractMember(node.Left);
                    Values[member.Name] = ValueVisitor.ExtractValue(node.Right);
                }
                else
                {
                    ThrowNotSupported(node.NodeType.ToString());
                }
                return base.VisitBinary(node);
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
        #endregion

        #region ValueVisitor
        [ExcludeFromGuardForNull]
        class ValueVisitor : ExpressionVisitor
        {
            private ValueVisitor()
            {
            }

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

            protected override Expression VisitIndex(IndexExpression node)
            {
                var instance = ExtractValueImpl(node.Object);
                var arguments = node.Arguments.Select(a => ExtractValueImpl(a)).ToArray();
                return Expression.Constant(node.Indexer.GetValue(instance, arguments), node.Type);
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                var instance = ExtractValueImpl(node.Object);
                var arguments = node.Arguments.Select(a => ExtractValueImpl(a)).ToArray();
                return Expression.Constant(node.Method.Invoke(instance, arguments), node.Type);
            }

            protected override Expression VisitNew(NewExpression node)
            {
                var arguments = node.Arguments.Select(a => ExtractValueImpl(a)).ToArray();
                return Expression.Constant(node.Constructor.Invoke(arguments));
            }

            static void ThrowNotSupported(string nodeDetails) =>
                throw new NotSupportedException($"Expression of type {nodeDetails} is not suppoerted in predicate reflector.");
        }
        #endregion
    }
}
