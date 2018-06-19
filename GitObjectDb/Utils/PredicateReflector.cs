using GitObjectDb.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Utils
{
    public class PredicateReflector<T>
    {
        public Expression<Predicate<T>> Predicate { get; }
        readonly PredicateVisitor _visitor;

        public PredicateReflector(Expression<Predicate<T>> predicate = null)
        {
            Predicate = predicate;
            _visitor = predicate != null ? CreateVisitor(predicate) : null;
        }

        PredicateVisitor CreateVisitor(Expression<Predicate<T>> predicate)
        {
            var visitor = new PredicateVisitor();
            visitor.Visit(predicate);
            return visitor;
        }

        public TValue ProcessArgument<TValue>(string name, TValue fallback) =>
            _visitor != null && _visitor.Values.TryGetValue(name, out var value) ? (TValue)value: fallback;

        class PredicateVisitor : ExpressionVisitor
        {
            public IDictionary<string, object> Values { get; } = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            protected override Expression VisitBinary(BinaryExpression node)
            {
                if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.TypeEqual)
                {
                    var member = ExtractMember(node.Left);
                    var value = ExtractValue(node.Right);

                    Values[member.Name] = value;
                }
                return base.VisitBinary(node);
            }

            static MemberInfo ExtractMember(Expression node)
            {
                var memberExpression = node as MemberExpression ?? throw new NotSupportedException("Member expressions expected in predicate.");
                if (!Attribute.IsDefined(memberExpression.Member, typeof(ModifiableAttribute))) throw new NotSupportedException($"Member expressions should be decorated with {nameof(ModifiableAttribute)} attribute.");
                return memberExpression.Member;
            }

            static object ExtractValue(Expression node)
            {
                switch (node)
                {
                    case ConstantExpression constant: return constant.Value;
                    case DefaultExpression @default: return null;
                    case MemberExpression member: return GetMemberValue(
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
        }
    }
}
