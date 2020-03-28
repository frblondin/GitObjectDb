using LibGit2Sharp;
using System.Collections.Generic;

namespace GitObjectDb
{
    /// <summary>Represents a series of node transformations.</summary>
    public interface ITransformationComposer
    {
        /// <summary>Gets the list of transformations.</summary>
        IList<ITransformation> Transformations { get; }

        /// <summary>Creates the specified node under an existing parent.</summary>
        /// <typeparam name="TNode">The type of the node being modified.</typeparam>
        /// <param name="node">The node to be added.</param>
        /// <param name="parent">The parent to insert the node into.</param>
        /// <returns>The node itself.</returns>
        TNode CreateOrUpdate<TNode>(TNode node, Node? parent = null)
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
        /// <param name="message">The message of the commit.</param>
        /// <param name="author">The author of the commit.</param>
        /// <param name="committer">The committer of the commit.</param>
        /// <param name="amendPreviousCommit">If set to <c>true</c>, previous commit will be amended.</param>
        /// <returns>The resulting commit.</returns>
        Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false);
    }
}