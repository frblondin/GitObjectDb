using System;
using System.Collections.Concurrent;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace GitObjectDb.Tools
{
    /// <summary>Simple node factory providing easy ways to set init-only property values.</summary>
    public static class NodeFactory
    {
        private static readonly ConcurrentDictionary<Type, Func<UniqueId, Node>> _cache = new();
        private static readonly PropertyInfo _nodeId = ExpressionReflector.GetProperty<Node>(n => n.Id);

        /// <summary>
        /// Creates a new instance of <paramref name="type"/> with given <paramref name="id"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of node to create.</param>
        /// <param name="id">The <see cref="UniqueId"/> of node to create.</param>
        /// <returns>The newly created node.</returns>
        public static Node Create(Type type, UniqueId id)
        {
            var factory = _cache.GetOrAdd(type, CreateFactory);
            return factory.Invoke(id);
        }

        private static Func<UniqueId, Node> CreateFactory(Type type)
        {
            var idArg = Parameter(typeof(UniqueId), "id");
            var constructor = type.GetConstructor(Type.EmptyTypes);
            var expression = Lambda<Func<UniqueId, Node>>(
                MemberInit(
                    New(constructor),
                    Bind(_nodeId, idArg)),
                idArg);
            return expression.Compile();
        }
    }
}
