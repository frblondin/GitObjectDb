using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Model
{
    /// <summary>Used to automatically map CLR classes to a model based on a set of conventions.</summary>
    public class ConventionBaseModelBuilder : ModelBuilder
    {
        private Dictionary<Type, NodeTypeDescription> _types = new Dictionary<Type, NodeTypeDescription>();

        /// <summary>Registers all types from an assembly inheriting from <see cref="Node"/>.</summary>
        /// <param name="assembly">Assembly to scan.</param>
        /// <param name="filter">Optional predicate that allows filtering retained types.</param>
        /// <returns>The current <see cref="ConventionBaseModelBuilder"/> instance.</returns>
        public ConventionBaseModelBuilder RegisterAssemblyTypes(Assembly assembly, Predicate<Type>? filter = null)
        {
            foreach (var type in assembly.GetTypes()
                .Where(t => typeof(Node).IsAssignableFrom(t) && (filter?.Invoke(t) ?? true)))
            {
                _types[type] = new NodeTypeDescription(type, GetTypeName(type));
            }
            return this;
        }

        private static string GetTypeName(Type type) =>
            type.GetCustomAttribute<GitFolderAttribute>()?.FolderName ?? type.Name;

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
            return new DataModel(_types.Values);
        }
    }
}
