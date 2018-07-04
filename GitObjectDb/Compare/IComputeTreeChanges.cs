using GitObjectDb.Models;
using LibGit2Sharp;
using System;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Compares to commits and computes the differences (additions, deletions...).
    /// </summary>
    public interface IComputeTreeChanges
    {
        /// <summary>
        /// Compares the specified commits.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance.</typeparam>
        /// <param name="oldTreeGetter">The old tree getter.</param>
        /// <param name="newTreeGetter">The new tree getter.</param>
        /// <returns>A <see cref="MetadataTreeChanges"/> containing all computed changes.</returns>
        MetadataTreeChanges Compare<TInstance>(Func<IRepository, Tree> oldTreeGetter, Func<IRepository, Tree> newTreeGetter)
            where TInstance : AbstractInstance;

        /// <summary>
        /// Compares two <see cref="AbstractInstance"/> instances and generates a new <see cref="TreeDefinition"/> instance containing the tree changes to be committed.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="newInstance">The new.</param>
        /// <param name="repository">The repository.</param>
        /// <returns>The <see cref="TreeDefinition"/> and <code>true</code> is any change was detected between the two instances.</returns>
        /// <exception cref="ArgumentNullException">
        /// original
        /// or
        /// new
        /// </exception>
        (TreeDefinition NewTree, bool AnyChange) Compare(AbstractInstance original, AbstractInstance newInstance, IRepository repository);
    }
}