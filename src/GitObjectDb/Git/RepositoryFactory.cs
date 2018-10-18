using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Git
{
    /// <inheritdoc/>
    internal sealed class RepositoryFactory : IRepositoryFactory
    {
        /// <inheritdoc/>
        public IRepository CreateRepository(RepositoryDescription description)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            var repository = new Repository(description.Path);
            if (description.Backend != null)
            {
                repository.ObjectDatabase.AddBackend(description.Backend, priority: 5);
            }

            return repository;
        }
    }
}
