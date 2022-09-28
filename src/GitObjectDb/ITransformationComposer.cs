using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace GitObjectDb;

/// <summary>Represents a series of node transformations.</summary>
public interface ITransformationComposer
{
    /// <summary>Gets the branch to apply the changes to.</summary>
    string BranchName { get; }

    /// <summary>Gets the list of transformations.</summary>
    IList<ITransformation> Transformations { get; }

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
    /// <returns>The item passed as argument.</returns>
    TItem Delete<TItem>(TItem item)
        where TItem : ITreeItem;

    /// <summary>Deletes the specified item path.</summary>
    /// <param name="path">The node path to update.</param>
    void Delete(DataPath path);

    /// <summary>Applies the transformation and store them in a new commit.</summary>
    /// <param name="description">The commit description.</param>
    /// <param name="beforeProcessing">Callback that gets invoked before processing each transformation.</param>
    /// <param name="type">Type of commit command to use.</param>
    /// <returns>The resulting commit.</returns>
    Commit Commit(CommitDescription description,
                  Action<ITransformation>? beforeProcessing = null,
                  CommitCommandType type = CommitCommandType.Auto);
}
