using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// A set of methods for instances of <see cref="IObjectRepositoryContainer"/>.
    /// </summary>
    internal static class IObjectRepositoryExtensions
    {
        /// <summary>
        /// Ensures that the given repository is the current one.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <exception cref="GitObjectDbException">
        /// The repository version is not currently managed by the container. This likely means that the repository was modified (commit, branch checkout...).
        /// or
        /// The repository is not currently managed by the container.
        /// </exception>
        internal static void EnsuresCurrentRepository(this IObjectRepository repository)
        {
            if (!repository.Container.Repositories.Contains(repository))
            {
                if (repository.Container.Repositories.Any(r => r.Id == repository.Id))
                {
                    throw new GitObjectDbException("The repository version is not currently managed by the container. This likely means that the repository was modified (commit, branch checkout...).");
                }
                throw new GitObjectDbException("The repository is not currently managed by the container.");
            }
        }

        /// <summary>
        /// Returns the result of the provided function processing.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="repository">The repository.</param>
        /// <param name="processor">The function.</param>
        /// <returns>The result of the function call.</returns>
        internal static TResult Execute<TResult>(this IObjectRepository repository, Func<IRepository, TResult> processor)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }
            if (repository.RepositoryDescription == null)
            {
                throw new GitObjectDbException($"No {nameof(repository.RepositoryDescription)} has been set on this instance.");
            }
            return repository.RepositoryProvider.Execute(repository.RepositoryDescription, processor);
        }

        /// <summary>
        /// Calls the provided function processing.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="processor">The function.</param>
        internal static void Execute(this IObjectRepository repository, Action<IRepository> processor)
        {
            if (repository == null)
            {
                throw new ArgumentNullException(nameof(repository));
            }
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }
            if (repository.RepositoryDescription == null)
            {
                throw new GitObjectDbException($"No {nameof(repository.RepositoryDescription)} has been set on this instance.");
            }
            repository.RepositoryProvider.Execute(repository.RepositoryDescription, processor);
        }
    }
}
