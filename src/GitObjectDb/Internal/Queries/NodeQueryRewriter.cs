using GitObjectDb.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace GitObjectDb.Internal.Queries
{
    internal class NodeQueryRewriter : ExpressionVisitor
    {
        private static readonly MethodInfo _queryableWhereMethodDefinition = ExpressionReflector.GetMethod(
            () => Queryable.Where<object>(default, o => true), returnGenericDefinition: true);

        private static readonly MethodInfo _queryableOfTypeMethodDefinition = ExpressionReflector.GetMethod(
            () => Queryable.OfType<object>(default), returnGenericDefinition: true);

        private static readonly ILookup<string, MethodInfo> _seqMethods =
            typeof(Enumerable).GetMethods(BindingFlags.Static | BindingFlags.Public).ToLookup((MethodInfo m) => m.Name);

        private readonly ISet<Expression> _nodeQueryFetcherExpressions = new HashSet<Expression>();
        private NodeQueryFetcher? _nodeQueryFetcher;

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            var expression = Visit(m.Object);
            var arguments = VisitExpressionList(m.Arguments);
            if (expression == m.Object && arguments == m.Arguments)
            {
                return m;
            }
            var typeArgs = m.Method.IsGenericMethod ? m.Method.GetGenericArguments() : null;
            if ((m.Method.IsStatic || m.Method.DeclaringType.IsAssignableFrom(expression.Type)) &&
                ArgsMatch(m.Method, arguments, typeArgs))
            {
                return Expression.Call(expression, m.Method, arguments);
            }
            if (m.Method.DeclaringType == typeof(Queryable))
            {
                return VisitQueryableMethodCall(m, expression, ref arguments, typeArgs);
            }
            else
            {
                var flags = BindingFlags.Static | (m.Method.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic);
                var method = FindMethod(m.Method.DeclaringType, m.Method.Name, arguments, typeArgs, flags);
                arguments = FixupQuotedArgs(method, arguments);
                return Expression.Call(expression, method, arguments);
            }
        }

        private Expression VisitQueryableMethodCall(MethodCallExpression m, Expression expression, ref IList<Expression> arguments, Type[]? typeArgs)
        {
            ProcessOfTypeMethodCall(m);
            var simplifiedExpression = ProcessWhereMethodCall(m);
            if (simplifiedExpression != m)
            {
                return simplifiedExpression;
            }
            else
            {
                var method = FindEnumerableMethod(m.Method.Name, arguments, typeArgs);
                arguments = FixupQuotedArgs(method, arguments);
                return Expression.Call(expression, method, arguments);
            }
        }

        private void ProcessOfTypeMethodCall(MethodCallExpression m)
        {
            if (m.Method.IsGenericMethod &&
                m.Method.GetGenericMethodDefinition() == _queryableOfTypeMethodDefinition)
            {
                if (m.Arguments[0] is ConstantExpression constant &&
                    constant.Value is NodeQuery nodeQuery)
                {
                    _nodeQueryFetcher = nodeQuery.Fetcher;
                    _nodeQueryFetcherExpressions.Add(m.Arguments[0]);
                    _nodeQueryFetcherExpressions.Add(m);
                }
                if (_nodeQueryFetcher != null && _nodeQueryFetcherExpressions.Contains(m.Arguments[0]))
                {
                    _nodeQueryFetcherExpressions.Add(m);
                    if (!_nodeQueryFetcher.IncompatiblePreedicate)
                    {
                        var body = NodeQueryFetcher.CreateIsOfTypePredicate(m.Method.GetGenericArguments()[0]);
                        _nodeQueryFetcher.AddPredicate(body);
                    }
                }
            }
        }

        private Expression ProcessWhereMethodCall(MethodCallExpression m)
        {
            if (m.Method.IsGenericMethod &&
                m.Method.GetGenericMethodDefinition() == _queryableWhereMethodDefinition)
            {
                if (m.Arguments[0] is ConstantExpression constant &&
                    constant.Value is NodeQuery nodeQuery)
                {
                    _nodeQueryFetcher = nodeQuery.Fetcher;
                    _nodeQueryFetcherExpressions.Add(m.Arguments[0]);
                    _nodeQueryFetcherExpressions.Add(m);
                }
                if (_nodeQueryFetcherExpressions.Contains(m.Arguments[0]))
                {
                    _nodeQueryFetcherExpressions.Add(m);
                    return VisitWhereMethodCall(m, m.Arguments[1]);
                }
            }
            return m;
        }

        private Expression VisitWhereMethodCall(MethodCallExpression m, Expression argument)
        {
            if (_nodeQueryFetcher != null && !_nodeQueryFetcher.IncompatiblePreedicate)
            {
                var quote = (UnaryExpression)argument;
                var predicate = (LambdaExpression)quote.Operand;
                var visitor = new NodeQueryPredicateVisitor(predicate.Parameters[0]);
                var body = visitor.Visit(predicate.Body);
                if (visitor.Incompatible)
                {
                    _nodeQueryFetcher.MarkAsIncompatiblePredicate();
                }
                else
                {
                    _nodeQueryFetcher.AddPredicate(body);
                    var result = FixupQuotedExpression(
                        m.Method.GetParameters()[0].ParameterType,
                        m.Arguments[0]);
                    _nodeQueryFetcherExpressions.Add(result);
                    return result;
                }
            }
            return m;
        }

        internal virtual IList<Expression> VisitExpressionList(IList<Expression> original)
        {
            var list = new Lazy<List<Expression>>();
            var i = 0;
            while (i < original.Count)
            {
                var expression = Visit(original[i]);
                if (list.IsValueCreated)
                {
                    list.Value.Add(expression);
                }
                else if (expression != original[i])
                {
                    list.Value.AddRange(original.Take(i));
                    list.Value.Add(expression);
                }
                i++;
            }
            return list.IsValueCreated ? list.Value : original;
        }

        private IList<Expression> FixupQuotedArgs(MethodInfo method, IList<Expression> arguments)
        {
            var parameters = method.GetParameters();
            var result = new Lazy<List<Expression>>();
            if (parameters.Length != 0)
            {
                var i = 0;
                while (i < parameters.Length)
                {
                    var expression = arguments[i];
                    var parameterInfo = parameters[i];
                    expression = FixupQuotedExpression(parameterInfo.ParameterType, expression);
                    if (!result.IsValueCreated && expression != arguments[i])
                    {
                        result.Value.AddRange(arguments.Take(i));
                    }
                    if (result.IsValueCreated)
                    {
                        result.Value.Add(expression);
                    }
                    i++;
                }
            }
            return result.IsValueCreated ? result.Value : arguments;
        }

        private Expression FixupQuotedExpression(Type type, Expression expression)
        {
            var result = expression;
            while (!type.IsAssignableFrom(result.Type))
            {
                if (result.NodeType != ExpressionType.Quote)
                {
                    if (!type.IsAssignableFrom(result.Type) &&
                        type.IsArray &&
                        result.NodeType == ExpressionType.NewArrayInit)
                    {
                        var c = StripExpression(result.Type);
                        if (type.IsAssignableFrom(c))
                        {
                            var elementType = type.GetElementType();
                            var newArrayExpression = (NewArrayExpression)result;
                            var list = new List<Expression>(newArrayExpression.Expressions.Count);
                            int i = 0;
                            while (i < newArrayExpression.Expressions.Count)
                            {
                                list.Add(FixupQuotedExpression(elementType, newArrayExpression.Expressions[i]));
                                i++;
                            }
                            expression = Expression.NewArrayInit(elementType, list);
                        }
                    }
                    return expression;
                }
                result = ((UnaryExpression)result).Operand;
            }
            return result;
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda) =>
            lambda;

        private static Type GetPublicType(Type t)
        {
            if (t.IsGenericType &&
                t.GetGenericTypeDefinition() == typeof(Lookup<,>).GetNestedType("Grouping", BindingFlags.Public | BindingFlags.NonPublic))
            {
                return typeof(IGrouping<,>).MakeGenericType(t.GetGenericArguments());
            }
            if (!t.IsNestedPrivate)
            {
                return t;
            }
            foreach (var type in t.GetInterfaces())
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return type;
                }
            }
            return typeof(IEnumerable).IsAssignableFrom(t) ?
                typeof(IEnumerable) :
                t;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            var enumerableQuery = c.Value as NodeQuery;
            if (enumerableQuery == null)
            {
                return c;
            }
            if (enumerableQuery.Enumerable != null)
            {
                var publicType = GetPublicType(enumerableQuery.Enumerable.GetType());
                return Expression.Constant(enumerableQuery.Enumerable, publicType);
            }
            return Visit(enumerableQuery.Expression);
        }

        protected override Expression VisitParameter(ParameterExpression p) =>
            p;

        private static MethodInfo FindEnumerableMethod(string name, IList<Expression> args, params Type[]? typeArgs)
        {
            var method = _seqMethods[name].FirstOrDefault(m => ArgsMatch(m, args, typeArgs));
            if (method == null)
            {
                throw new InvalidOperationException($"There is no method '{name}' on type '{typeof(Enumerable)}' that matches the specified arguments.");
            }
            if (typeArgs != null)
            {
                return method.MakeGenericMethod(typeArgs);
            }
            return method;
        }

        internal static MethodInfo FindMethod(Type type, string name, IList<Expression> args, Type[]? typeArgs, BindingFlags flags)
        {
            var method = (from m in type.GetMethods(flags)
                          where m.Name == name
                          where ArgsMatch(m, args, typeArgs)
                          select m).FirstOrDefault();
            if (method == null)
            {
                throw new InvalidOperationException($"There is no method '{name}' on type '{typeof(Enumerable)}' that matches the specified arguments.");
            }
            return typeArgs != null ? method.MakeGenericMethod(typeArgs) : method;
        }

        private static bool ArgsMatch(MethodInfo m, IList<Expression> args, Type[]? typeArgs)
        {
            var parameters = m.GetParameters();
            if (parameters.Length != args.Count)
            {
                return false;
            }
            if (!m.IsGenericMethod && typeArgs != null && typeArgs.Length != 0)
            {
                return false;
            }
            if (!m.IsGenericMethodDefinition && m.IsGenericMethod && m.ContainsGenericParameters)
            {
                m = m.GetGenericMethodDefinition();
            }
            if (m.IsGenericMethodDefinition)
            {
                if (typeArgs == null || typeArgs.Length == 0)
                {
                    return false;
                }
                if (m.GetGenericArguments().Length != typeArgs.Length)
                {
                    return false;
                }
                m = m.MakeGenericMethod(typeArgs);
                parameters = m.GetParameters();
            }
            int i = 0;
            int count = args.Count;
            while (i < count)
            {
                var type = parameters[i].ParameterType;
                if (type == null)
                {
                    return false;
                }
                if (type.IsByRef)
                {
                    type = type.GetElementType();
                }
                var expression = args[i];
                if (!type.IsAssignableFrom(expression.Type))
                {
                    if (expression.NodeType == ExpressionType.Quote)
                    {
                        expression = ((UnaryExpression)expression).Operand;
                    }
                    if (!type.IsAssignableFrom(expression.Type) && !type.IsAssignableFrom(StripExpression(expression.Type)))
                    {
                        return false;
                    }
                }
                i++;
            }
            return true;
        }

        private static Type StripExpression(Type type)
        {
            var elementType = type.IsArray ? type.GetElementType() : type;
            var expressionType = TypeHelper.FindGenericType(typeof(Expression<>), elementType);
            if (expressionType != null)
            {
                elementType = expressionType.GetGenericArguments()[0];
            }
            if (!type.IsArray)
            {
                return type;
            }
            return type.GetArrayRank() != 1 ?
                elementType.MakeArrayType(type.GetArrayRank()) :
                elementType.MakeArrayType();
        }
    }
}
