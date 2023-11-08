using GitObjectDb.Comparison;
using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace GitObjectDb.Model;

/// <summary>Updates a deprecated node type into a new type.</summary>
/// <param name="old">The node to be updated.</param>
/// <param name="targetType">The target updated type.</param>
/// <returns>The updated node.</returns>
public delegate Node UpdateDeprecatedNode(Node old, Type targetType);

/// <summary>Semantic representation of a model.</summary>
public interface IDataModel
{
    /// <summary>Gets the collection of node types that are contained in this model.</summary>
    IReadOnlyList<NodeTypeDescription> NodeTypes { get; }

    /// <summary>Gets the default comparison policy that should apply for the model.</summary>
    ComparisonPolicy DefaultComparisonPolicy { get; }

    /// <summary>Gets the function that can update deprecated node to a target type.</summary>
    UpdateDeprecatedNode? DeprecatedNodeUpdater { get; }

    /// <summary>Gets the new type that should replace a deprecated node type.</summary>
    /// <param name="nodeType">The node type, potentially deprecated.</param>
    /// <returns>The new type that should replace the deprecated one.</returns>
    Type? GetNewTypeIfDeprecated(Type nodeType);

    /// <summary>Gets the type description from model.</summary>
    /// <param name="type">The type expected to be described in model.</param>
    /// <returns>The type description.</returns>
    NodeTypeDescription GetDescription(Type type);

    /// <summary>Gets the types that a given <paramref name="folderName"/> should contain.</summary>
    /// <param name="folderName">The name of the folder.</param>
    /// <returns>The matching types.</returns>
    IEnumerable<NodeTypeDescription> GetTypesMatchingFolderName(string folderName);

    /// <summary>Updates the <paramref name="deprecated"/> node to type <paramref name="newType"/>.</summary>
    /// <param name="deprecated">The node to update.</param>
    /// <param name="newType">The type to convert the node to.</param>
    /// <returns>The updated node.</returns>
    Node UpdateDeprecatedNode(Node deprecated, Type newType);

    /// <summary>Updates the base node properties.</summary>
    /// <param name="node">The node to update.</param>
    /// <param name="treeId">The id of the tree where the stream is originated from.</param>
    /// <param name="path">The new path value.</param>
    void UpdateBaseProperties(Node node, ObjectId? treeId, DataPath? path);
}
