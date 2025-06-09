using GitDotNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitObjectDb;

/// <summary>Represents a series of node transformations.</summary>
public interface IChangeComposer
{
    /// <summary>Gets the branch to apply the changes to.</summary>
    string BranchName { get; }

    /// <summary>Creates the specified node under an existing parent.</summary>
    /// <typeparam name="TNode">The type of the node being modified.</typeparam>
    /// <param name="node">The node to be added.</param>
    /// <param name="parent">The parent to insert the node into.</param>
    /// <returns>The node itself.</returns>
    Task<TNode> CreateOrUpdateAsync<TNode>(TNode node, Node? parent)
        where TNode : Node;

    /// <summary>Creates the specified node under an existing parent.</summary>
    /// <typeparam name="TNode">The type of the node being modified.</typeparam>
    /// <param name="node">The node to be added.</param>
    /// <param name="parent">The parent to insert the node into.</param>
    /// <returns>The node itself.</returns>
    Task<TNode> CreateOrUpdateAsync<TNode>(TNode node, DataPath? parent)
        where TNode : Node;

    /// <summary>Creates the specified node under an existing parent.</summary>
    /// <typeparam name="TNode">The type of the node being modified.</typeparam>
    /// <param name="node">The node to be added.</param>
    /// <returns>The node itself.</returns>
    Task<TNode> CreateOrUpdateAsync<TNode>(TNode node)
        where TNode : Node;

    /// <summary>Updates the specified resource.</summary>
    /// <param name="resource">The item to update.</param>
    /// <returns>The resource itself.</returns>
    Task<Resource> CreateOrUpdateAsync(Resource resource);

    /// <summary>Deletes the specified item.</summary>
    /// <typeparam name="TItem">The type of the item being modified.</typeparam>
    /// <param name="item">The node to update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteAsync<TItem>(TItem item)
        where TItem : TreeItem;

    /// <summary>Deletes the specified item path.</summary>
    /// <param name="path">The node path to update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RevertAsync(DataPath path);

    /// <summary>Renames the specified item to a new path.</summary>
    /// <param name="item">The item to be renamed.</param>
    /// <param name="newPath">The new item path.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RenameAsync(TreeItem item, DataPath newPath);
}

/// <summary>Represents a series of node transformations.</summary>
public interface IChangeComposerWithCommit : IChangeComposer
{
    /// <summary>Gets all defined transformations.</summary>
    IDictionary<DataPath, ITransformation> Transformations { get; }

    /// <summary>Applies the transformation and store them in a new commit.</summary>
    /// <param name="description">The commit description.</param>
    /// <param name="beforeProcessing">Callback that gets invoked before processing each transformation.</param>
    /// <returns>The resulting commit.</returns>
    Task<CommitEntry> CommitAsync(CommitDescription description,
        Action<ITransformation>? beforeProcessing = null);
}