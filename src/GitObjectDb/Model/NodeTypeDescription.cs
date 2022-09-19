using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Model;

/// <summary>Provides a description of a node type.</summary>
[DebuggerDisplay("Name = {Name}, Type = {Type}")]
public sealed class NodeTypeDescription : IEquatable<NodeTypeDescription>
{
    private readonly List<NodeTypeDescription> _children = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="NodeTypeDescription"/> class.
    /// </summary>
    /// <param name="type">The CLR <see cref="Type"/> of the node.</param>
    /// <param name="name">The name of the node type.</param>
    /// <param name="useNodeFolders">Whether node should be stored in a nested folder (FolderName/NodeId/data.json) or not (FolderName/NodeId.json).</param>
    public NodeTypeDescription(Type type, string name, bool? useNodeFolders = null)
    {
        ValidateType(type);

        Type = type;
        Name = name;

        SearchableProperties = GetSearchableProperties(type).ToImmutableList();
        UseNodeFolders = useNodeFolders ??
                         GitFolderAttribute.Get(type)?.UseNodeFolders ??
                         GitFolderAttribute.DefaultUseNodeFoldersValue;
    }

    /// <summary>Gets the CLR <see cref="Type"/> of the node.</summary>
    public Type Type { get; }

    /// <summary>Gets the name of the node type.</summary>
    public string Name { get; }

    /// <summary>Gets the list of searchable properties.</summary>
    public IList<PropertyInfo> SearchableProperties { get; }

    /// <summary>Gets the children that this node can contain.</summary>
    public IList<NodeTypeDescription> Children => _children.AsReadOnly();

    /// <summary>Gets a value indicating whether node should be stored in a nested folder (FolderName/NodeId/data.json) or not (FolderName/NodeId.json).</summary>
    public bool UseNodeFolders { get; }

    private static void ValidateType(Type type)
    {
        if (type.IsGenericTypeDefinition)
        {
            throw new ArgumentException($"Type {type} is a generic type definition.");
        }
        if (type != typeof(Node) && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) is null)
        {
            throw new ArgumentException($"No parameterless constructor found for type {type}.");
        }
        if (!typeof(Node).IsAssignableFrom(type))
        {
            throw new ArgumentException($"{nameof(type)} should inherit from {nameof(Node)}.");
        }
    }

    private static IEnumerable<PropertyInfo> GetSearchableProperties(Type type)
    {
        return type.GetProperties().Where(IsSearchable);

        static bool IsSearchable(PropertyInfo property) =>
            property.GetCustomAttribute<IsSearchableAttribute>()?.Searchable ??
            property.PropertyType == typeof(string);
    }

    internal void AddChild(NodeTypeDescription nodeType)
    {
        if (!_children.Contains(nodeType))
        {
            _children.Add(nodeType);
        }
    }

    /// <inheritdoc />
    public bool Equals(NodeTypeDescription? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) ||
               Type == other.Type;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is NodeTypeDescription description && Equals(description);

    /// <inheritdoc />
    public override int GetHashCode() =>
        Type.GetHashCode();
}
