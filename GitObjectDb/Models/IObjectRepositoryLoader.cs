using GitObjectDb.Git;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Loads a <see cref="AbstractObjectRepository"/> instance from a Git repository.
    /// </summary>
    public interface IObjectRepositoryLoader
    {
        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <param name="childrenResolver">The children resolver.</param>
        /// <returns>A new <see cref="JsonSerializer"/>.</returns>
        JsonSerializer GetJsonSerializer(ChildrenResolver childrenResolver = null);

        /// <summary>
        /// Loads the instance from a Git repository.
        /// </summary>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit.</param>
        /// <returns>The loaded instance.</returns>
        AbstractObjectRepository LoadFrom(RepositoryDescription repositoryDescription, ObjectId commitId = null);

        /// <summary>
        /// Loads the instance from a Git repository.
        /// </summary>
        /// <typeparam name="TRepository">The type of the repository.</typeparam>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit.</param>
        /// <returns>The loaded instance.</returns>
        TRepository LoadFrom<TRepository>(RepositoryDescription repositoryDescription, ObjectId commitId = null)
            where TRepository : AbstractObjectRepository;
    }
}