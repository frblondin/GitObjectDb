using FluentValidation.Results;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    /// <inheritdoc />
    public abstract class ObjectRepositoryContainer : IObjectRepositoryContainer
    {
        /// <inheritdoc />
        public abstract string Path { get; }

        /// <inheritdoc />
        public IEnumerable<IObjectRepository> Repositories => GetRepositoriesCore();

        /// <summary>
        /// Reloads the repository and refreshes the reference in the container.
        /// </summary>
        /// <param name="previousRepository">The previous repository.</param>
        /// <param name="commit">The commit.</param>
        /// <returns>The loaded repository.</returns>
        internal abstract IObjectRepository ReloadRepository(IObjectRepository previousRepository, ObjectId commit);

        /// <inheritdoc />
        public abstract IObjectRepository TryGetRepository(Guid id);

        /// <inheritdoc />
        public abstract ValidationResult Validate(ValidationRules rules = ValidationRules.All);

        /// <summary>
        /// Gets the repositories being managed by the container.
        /// </summary>
        /// <returns>The repositories.</returns>
        protected abstract IEnumerable<IObjectRepository> GetRepositoriesCore();

        /// <summary>
        /// Ensures that the head tip refers to the right commit.
        /// </summary>
        /// <param name="current">The repository.</param>
        /// <exception cref="GitObjectDbException">The current head commit id is different from the commit used by current repository.</exception>
        internal static void EnsureHeadCommit(IObjectRepository current) =>
            current.RepositoryProvider.Execute(current.RepositoryDescription, r => EnsureHeadCommit(r, current));

        /// <summary>
        /// Ensures that the head tip refers to the right commit.
        /// </summary>
        /// <param name="repository">The git repository.</param>
        /// <param name="current">The repository.</param>
        /// <exception cref="GitObjectDbException">The current head commit id is different from the commit used by current repository.</exception>
        protected static void EnsureHeadCommit(IRepository repository, IObjectRepository current)
        {
            if (!repository.Head.Tip.Id.Equals(current.CommitId))
            {
                throw new GitObjectDbException("The current head commit id is different from the commit used by current instance.");
            }
        }
    }
}
