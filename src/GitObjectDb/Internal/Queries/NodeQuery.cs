using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Internal.Queries
{
#pragma warning disable SA1402 // File may only contain a single type
    internal abstract class NodeQuery
    {
        internal abstract NodeQueryFetcher? Fetcher { get; }

        public IEnumerable? Enumerable { get; protected set; }

        public abstract Expression Expression { get; }

        internal static IQueryable Create(Type elementType, IEnumerable sequence) =>
            (IQueryable)Activator.CreateInstance(
                typeof(EnumerableQuery<>).MakeGenericType(new[] { elementType }),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { sequence }, null);

        internal static IQueryable Create(Type elementType, Expression expression) =>
            (IQueryable)Activator.CreateInstance(
                typeof(EnumerableQuery<>).MakeGenericType(new[] { elementType }),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { expression }, null);
    }

    internal class NodeQuery<T> : NodeQuery, IQueryable<T>, IQueryProvider
    {
        public NodeQuery(NodeQueryFetcher fetcher)
        {
            Fetcher = fetcher;
            Enumerable = fetcher.Fetch();
            Expression = Expression.Constant(this);
        }

        public NodeQuery(Expression expression)
        {
            Expression = expression;
        }

        internal override NodeQueryFetcher? Fetcher { get; }

        public Type ElementType => typeof(T);

        public override Expression Expression { get; }

        public IQueryProvider Provider => this;

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            var type = TypeHelper.FindGenericType(typeof(IQueryable<>), expression.Type);
            if (type == null)
            {
                throw new ArgumentException($"Argument {nameof(expression)} is not valid.");
            }

            return Create(type.GetGenericArguments()[0], expression);
        }

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            if (!typeof(IQueryable<TElement>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException($"Argument {nameof(expression)} is not valid.");
            }

            return new NodeQuery<TElement>(expression);
        }

        public object? Execute(Expression expression) =>
            NodeQueryExecutor.Create(expression).ExecuteBoxed();

        public TResult Execute<TResult>(Expression expression)
        {
            if (!typeof(TResult).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentException($"Argument {nameof(expression)} is not valid.");
            }
            return new NodeQueryExecutor<TResult>(expression).Execute();
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (Enumerable == null)
            {
                Enumerable = new NodeQueryExecutor<IEnumerable<T>>(Expression).Execute();
            }
            return (IEnumerator<T>)Enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public override string ToString()
        {
            if (!(Expression is ConstantExpression constant) || constant.Value != this)
            {
                return Expression.ToString();
            }
            return Enumerable?.ToString() ?? "null";
        }
    }

    internal abstract class NodeQueryExecutor
    {
        protected NodeQueryExecutor()
        {
        }

        internal abstract object? ExecuteBoxed();

        internal static NodeQueryExecutor Create(Expression expression) =>
            (NodeQueryExecutor)Activator.CreateInstance(
                typeof(EnumerableExecutor<>).MakeGenericType(new[] { expression.Type }),
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new object[] { expression },
                null);
    }

    internal class NodeQueryExecutor<T> : NodeQueryExecutor
    {
        private readonly Expression _expression;
        private readonly Lazy<Func<T>> _func;

        public NodeQueryExecutor(Expression expression)
        {
            _expression = expression;
            _func = new Lazy<Func<T>>(Create);
        }

        internal override object? ExecuteBoxed() =>
            _func.Value.Invoke();

        internal T Execute() =>
            _func.Value.Invoke();

        private Func<T> Create()
        {
            var rewriter = new NodeQueryRewriter();
            var body = rewriter.Visit(_expression);
            var expression = Expression.Lambda<Func<T>>(body, null);
            return expression.Compile();
        }
    }
}
