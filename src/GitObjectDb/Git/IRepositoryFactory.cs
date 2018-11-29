using LibGit2Sharp;

namespace GitObjectDb.Git
{
    /// <summary>
    /// Returns <see cref="IRepository"/> instances.
    /// </summary>
    internal interface IRepositoryFactory
    {
        /// <summary>
        /// Creates a new repository from a <see cref="RepositoryDescription"/>.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>The newly created <see cref="IRepository"/> instance.</returns>
        IRepository CreateRepository(RepositoryDescription description);
    }
}