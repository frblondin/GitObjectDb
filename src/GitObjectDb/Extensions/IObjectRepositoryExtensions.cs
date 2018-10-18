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
    }
}
