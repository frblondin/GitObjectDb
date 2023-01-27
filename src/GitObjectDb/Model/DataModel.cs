using Fasterflect;
using GitObjectDb.Comparison;
using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Model;

/// <summary>Semantic representation of a model.</summary>
internal class DataModel : IDataModel
{
    private static readonly MemberSetter _treeIdSetter = Reflect.PropertySetter(
        ExpressionReflector.GetProperty<Node>(n => n.TreeId));

    private static readonly MemberSetter _embeddedResourceSetter = Reflect.PropertySetter(
        ExpressionReflector.GetProperty<Node>(n => n.EmbeddedResource));

    private readonly ILookup<string, NodeTypeDescription> _folderNames;
    private readonly Dictionary<Type, Type> _deprecatedTypes;

    internal DataModel(IReadOnlyList<NodeTypeDescription> nodeTypes, UpdateDeprecatedNode? deprecatedNodeUpdater)
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

    public IReadOnlyList<NodeTypeDescription> NodeTypes { get; }

    public ComparisonPolicy DefaultComparisonPolicy { get; }

    public UpdateDeprecatedNode? DeprecatedNodeUpdater { get; }

    public IEnumerable<NodeTypeDescription> GetTypesMatchingFolderName(string folderName) =>
        _folderNames[folderName];

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
            var deprecatedDescription = GetDescription(kvp.Key);
            var newDescription = GetDescription(kvp.Value);

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

    public NodeTypeDescription GetDescription(Type type) =>
        NodeTypes.FirstOrDefault(t => t.Type == type) ??
        throw new GitObjectDbException($"Type {type} could not be found in model.");

    public Type? GetNewTypeIfDeprecated(Type nodeType) =>
        _deprecatedTypes.TryGetValue(nodeType, out var newType) ? newType : null;

    public Node UpdateDeprecatedNode(Node deprecated, Type newType)
    {
        if (DeprecatedNodeUpdater is null)
        {
            throw new GitObjectDbException("No deprecated node updater defined in model.");
        }
        var updated = DeprecatedNodeUpdater(deprecated, newType) ??
            throw new GitObjectDbException("Deprecated node updater did not return any value.");
        if (!newType.IsInstanceOfType(updated))
        {
            throw new GitObjectDbException($"Deprecated node updater did not return a value of type '{newType}'.");
        }
        if (updated.Id != deprecated.Id)
        {
            throw new GitObjectDbException($"Updated node does not have the same id.");
        }
        UpdateBaseProperties(updated, deprecated.TreeId, deprecated.Path, deprecated.EmbeddedResource);
        return updated;
    }

    public void UpdateBaseProperties(Node node, ObjectId? treeId, DataPath? path, string? embeddedResource)
    {
        node.Path = path;
        _treeIdSetter.Invoke(node, treeId);
        _embeddedResourceSetter.Invoke(node, embeddedResource);
    }
}
