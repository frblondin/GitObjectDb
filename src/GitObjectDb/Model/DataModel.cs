using GitObjectDb.Comparison;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Model
{
    /// <summary>Semantic representation of a model.</summary>
    internal class DataModel : IDataModel
    {
        private readonly ILookup<string, NodeTypeDescription> _folderNames;
        private readonly Dictionary<Type, Type> _deprecatedTypes;

        internal DataModel(IEnumerable<NodeTypeDescription> nodeTypes, UpdateDeprecatedNode? deprecatedNodeUpdater)
        {
            NodeTypes = nodeTypes;
            DeprecatedNodeUpdater = deprecatedNodeUpdater;

            DefaultComparisonPolicy = ComparisonPolicy.CreateDefault(this);
            _folderNames = NodeTypes.ToLookup(n => n.Name);
            _deprecatedTypes = (from t in NodeTypes
                                let deprecatedTypeAttribute = t.Type.GetCustomAttribute<IsDeprecatedNodeTypeAttribute>()
                                where deprecatedTypeAttribute is not null
                                select (t.Type, deprecatedTypeAttribute.NewType)).ToDictionary(i => i.Type, i => i.NewType);

            ValidateFolderNames();
            ValidateDeprecatedTypes();
        }

        public IEnumerable<NodeTypeDescription> NodeTypes { get; }

        public ComparisonPolicy DefaultComparisonPolicy { get; }

        public UpdateDeprecatedNode? DeprecatedNodeUpdater { get; }

        public IEnumerable<NodeTypeDescription> GetTypesMatchingFolderName(string folderName) =>
            _folderNames[folderName];

        public Type? GetNewTypeIfDeprecated(Type nodeType) =>
            _deprecatedTypes.TryGetValue(nodeType, out var newType) ? newType : null;

        private void ValidateFolderNames()
        {
            foreach (var folderGroup in _folderNames)
            {
                var useNodeFolderValues = folderGroup.GroupBy(t => t.UseNodeFolders);
                if (useNodeFolderValues.Count() > 1)
                {
                    throw new GitObjectDbException(
                        $"Same folder name cannot be used by various types using " +
                        $"different values for {nameof(GitFolderAttribute.UseNodeFolders)}: " +
                        $"{string.Join(", ", folderGroup.Select(t => t.Type))}.");
                }
            }
        }

        private void ValidateDeprecatedTypes()
        {
            foreach (var kvp in _deprecatedTypes)
            {
                var deprecatedDescription = NodeTypes.First(d => d.Type == kvp.Key);
                var newDescription = NodeTypes.First(d => d.Type == kvp.Value);

                if (deprecatedDescription.UseNodeFolders != newDescription.UseNodeFolders)
                {
                    throw new GitObjectDbException($"Deprecated and new types should use the same {nameof(NodeTypeDescription.UseNodeFolders)}.");
                }
                if (!deprecatedDescription.Name.Equals(newDescription.Name, StringComparison.Ordinal))
                {
                    throw new GitObjectDbException($"Deprecated and new types should use the same {nameof(NodeTypeDescription.Name)}.");
                }
            }
        }
    }
}
