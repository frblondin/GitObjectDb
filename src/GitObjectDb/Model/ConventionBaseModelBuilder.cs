using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Model
{
    /// <summary>Used to automatically map CLR classes to a model based on a set of conventions.</summary>
    public class ConventionBaseModelBuilder : ModelBuilder
    {
        private readonly Dictionary<Type, NodeTypeDescription> _types = new Dictionary<Type, NodeTypeDescription>();

        /// <summary>Gets the type descriptions.</summary>
        public IReadOnlyCollection<NodeTypeDescription> Types => _types.Values;

        /// <summary>Registers all types from an assembly inheriting from <see cref="Node"/>.</summary>
        /// <param name="assembly">Assembly to scan.</param>
        /// <param name="filter">Optional predicate that allows filtering retained types.</param>
        /// <returns>The current <see cref="ConventionBaseModelBuilder"/> instance.</returns>
        public ConventionBaseModelBuilder RegisterAssemblyTypes(Assembly assembly, Predicate<Type>? filter = null)
        {
            foreach (var type in from t in assembly.GetTypes()
                                 where !t.IsAbstract &&
                                       !t.IsGenericTypeDefinition &&
                                       typeof(Node).IsAssignableFrom(t) &&
                                       (filter?.Invoke(t) ?? true)
                                 select t)
            {
                _types[type] = new NodeTypeDescription(type, GetTypeName(type));
            }
            return this;
        }

        /// <summary>Register a type inheriting from <see cref="Node"/>.</summary>
        /// <typeparam name="TNode">The type of node to be added to the model.</typeparam>
        /// <returns>The current <see cref="ConventionBaseModelBuilder"/> instance.</returns>
        public ConventionBaseModelBuilder RegisterType<TNode>()
            where TNode : Node
        {
            _types[typeof(TNode)] = new NodeTypeDescription(typeof(TNode), GetTypeName(typeof(TNode)));
            return this;
        }

        /// <summary>Register a type inheriting from <see cref="Node"/>.</summary>
        /// <param name="types">The types of node to be added to the model.</param>
        /// <returns>The current <see cref="ConventionBaseModelBuilder"/> instance.</returns>
        public ConventionBaseModelBuilder RegisterTypes(IEnumerable<NodeTypeDescription> types)
        {
            foreach (var description in types)
            {
                _types[description.Type] = description;
            }
            return this;
        }

        private static string GetTypeName(Type type) =>
            GitFolderAttribute.Get(type)?.FolderName ?? $"{type.Name}s";

        /// <inheritdoc/>
        public override IDataModel Build()
        {
            foreach (var type in _types.Values)
            {
                foreach (var child in type.Type.GetCustomAttributes<HasChildAttribute>())
                {
                    if (!_types.TryGetValue(child.ChildType, out var childDescription))
                    {
                        throw new GitObjectDbException($"Child type {child.ChildType} could not be found in registered types.");
                    }
                    type.AddChild(childDescription);
                }
            }
            return new DataModel(_types.Values, DeprecatedNodeUpdater);
        }
    }
}
