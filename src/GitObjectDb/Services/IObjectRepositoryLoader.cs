using GitObjectDb.Git;
using GitObjectDb.Models;
using LibGit2Sharp;
using System;

namespace GitObjectDb.Services
{
    /// <summary>
    /// Loads a <see cref="IObjectRepository"/> instance from a Git repository.
    /// </summary>
    public interface IObjectRepositoryLoader
    {
        /// <summary>
        /// Loads the instance from a Git repository.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="repository">The (possibly remote) repository to clone from. See the <see href="https://git-scm.com/docs/git-clone#URLS">Git urls</see> section below for more information on specifying repositories.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit to load, HEAD tip is loaded if not provided.</param>
        /// <returns>The loaded instance.</returns>
        IObjectRepository Clone(IObjectRepositoryContainer container, string repository, RepositoryDescription repositoryDescription, ObjectId commitId = null);

        /// <summary>
        /// Loads the instance from a Git repository.
        /// </summary>
        /// <typeparam name="TRepository">The type of the repository.</typeparam>
        /// <param name="container">The container.</param>
        /// <param name="repository">The (possibly remote) repository to clone from. See the <see href="https://git-scm.com/docs/git-clone#URLS">Git urls</see> section below for more information on specifying repositories.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit, HEAD tip is loaded if not provided.</param>
        /// <returns>The loaded instance.</returns>
        TRepository Clone<TRepository>(IObjectRepositoryContainer<TRepository> container, string repository, RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TRepository : class, IObjectRepository;

        /// <summary>
        /// Loads the instance from a Git repository.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit to load, HEAD tip is loaded if not provided.</param>
        /// <returns>The loaded instance.</returns>
        IObjectRepository LoadFrom(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription, ObjectId commitId = null);

        /// <summary>
        /// Loads the instance from a Git repository.
        /// </summary>
        /// <typeparam name="TRepository">The type of the repository.</typeparam>
        /// <param name="container">The container.</param>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit to load, HEAD tip is loaded if not provided.</param>
        /// <returns>The loaded instance.</returns>
        TRepository LoadFrom<TRepository>(IObjectRepositoryContainer<TRepository> container, RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TRepository : class, IObjectRepository;
    }
}