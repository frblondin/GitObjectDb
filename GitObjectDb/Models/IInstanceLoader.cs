using LibGit2Sharp;
using System;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Loads instance from a Git repository.
    /// </summary>
    public interface IInstanceLoader
    {
        /// <summary>
        /// Loads the instance from a Git repository.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance.</typeparam>
        /// <param name="repositoryFactory">The repository factory.</param>
        /// <param name="tree">The tree.</param>
        /// <returns>The loaded instance.</returns>
        TInstance LoadFrom<TInstance>(Func<IRepository> repositoryFactory, Func<IRepository, Tree> tree)
            where TInstance : AbstractInstance;
    }
}