using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace GitObjectDb;

/// <summary>Represents a series of node transformations.</summary>
public interface ITransformationComposer
{
    /// <summary>Gets the branch to apply the changes to.</summary>
    string BranchName { get; }

    /// <summary>Creates the specified node under an existing parent.</summary>
    /// <typeparam name="TNode">The type of the node being modified.</typeparam>
    /// <param name="node">The node to be added.</param>
    /// <param name="parent">The parent to insert the node into.</param>
    /// <returns>The node itself.</returns>
    TNode CreateOrUpdate<TNode>(TNode node, Node? parent)
        where TNode : Node;

    /// <summary>Creates the specified node under an existing parent.</summary>
    /// <typeparam name="TNode">The type of the node being modified.</typeparam>
    /// <param name="node">The node to be added.</param>
    /// <param name="parent">The parent to insert the node into.</param>
    /// <returns>The node itself.</returns>
    TNode CreateOrUpdate<TNode>(TNode node, DataPath? parent)
        where TNode : Node;

    /// <summary>Creates the specified node under an existing parent.</summary>
    /// <typeparam name="TNode">The type of the node being modified.</typeparam>
    /// <param name="node">The node to be added.</param>
    /// <returns>The node itself.</returns>
    TNode CreateOrUpdate<TNode>(TNode node)
        where TNode : Node;

    /// <summary>Updates the specified resource.</summary>
    /// <param name="resource">The item to update.</param>
    /// <returns>The resource itself.</returns>
    Resource CreateOrUpdate(Resource resource);

    /// <summary>Deletes the specified item.</summary>
    /// <typeparam name="TItem">The type of the item being modified.</typeparam>
    /// <param name="item">The node to update.</param>
    void Delete<TItem>(TItem item)
        where TItem : TreeItem;

    /// <summary>Deletes the specified item path.</summary>
    /// <param name="path">The node path to update.</param>
    void Revert(DataPath path);

    /// <summary>Renames the specified item to a new path.</summary>
    /// <param name="item">The item to be renamed.</param>
    /// <param name="newPath">The new item path.</param>
    void Rename(TreeItem item, DataPath newPath);
}

/// <summary>Represents a series of node transformations.</summary>
public interface ITransformationComposerWithCommit : ITransformationComposer
{
    /// <summary>Gets all defined transformations.</summary>
    IReadOnlyDictionary<DataPath, ITransformation> Transformations { get; }

    /// <summary>Applies the transformation and store them in a new commit.</summary>
    /// <param name="description">The commit description.</param>
    /// <param name="beforeProcessing">Callback that gets invoked before processing each transformation.</param>
    /// <returns>The resulting commit.</returns>
    Commit Commit(CommitDescription description,
                  Action<ITransformation>? beforeProcessing = null);
}