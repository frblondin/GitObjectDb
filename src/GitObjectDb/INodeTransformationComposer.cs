using LibGit2Sharp;
using System.Collections.Generic;

namespace GitObjectDb
{
    /// <summary>Represents a series of node transformations.</summary>
    public interface INodeTransformationComposer
    {
        /// <summary>Gets the list of transformations.</summary>
        IList<INodeTransformation> Transformations { get; }

        /// <summary>Creates the specified node under an existing parent.</summary>
        /// <param name="node">The node to be added.</param>
        /// <param name="parent">The parent to insert the node into.</param>
        /// <returns>The current <see cref="INodeTransformationComposer"/>.</returns>
        INodeTransformationComposer CreateOrUpdate(Node node, Node parent = null);

        /// <summary>
        /// Updates the specified resource.
        /// </summary>
        /// <param name="resource">The item to update.</param>
        /// <returns>The current <see cref="INodeTransformationComposer"/>.</returns>
        INodeTransformationComposer CreateOrUpdate(Resource resource);

        /// <summary>
        /// Deletes the specified item.
        /// </summary>
        /// <param name="item">The node to update.</param>
        /// <returns>The current <see cref="INodeTransformationComposer"/>.</returns>
        INodeTransformationComposer Delete(ITreeItem item);

        /// <summary>Applies the transformation and store them in a new commit.</summary>
        /// <param name="message">The message of the commit.</param>
        /// <param name="author">The author of the commit.</param>
        /// <param name="committer">The committer of the commit.</param>
        /// <param name="amendPreviousCommit">If set to <c>true</c>, previous commit will be amended.</param>
        /// <returns>The resulting commit.</returns>
        Commit Commit(string message, Signature author, Signature committer, bool amendPreviousCommit = false);
    }
}