using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Services
{
    /// <summary>
    /// Service that searches for information within a repository.
    /// </summary>
    public interface IObjectRepositorySearch
    {
        /// <summary>
        /// Searches for the specified content in the repository.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="content">The content.</param>
        /// <returns>All objects containing the <paramref name="content"/> value.</returns>
        IEnumerable<IMetadataObject> Grep(IObjectRepository repository, string content);

        /// <summary>
        /// Searches for the specified content in the repository.
        /// </summary>
        /// <typeparam name="TRepository">The type of the object repository.</typeparam>
        /// <param name="container">The container.</param>
        /// <param name="content">The content.</param>
        /// <returns>All objects containing the <paramref name="content"/> value.</returns>
        IEnumerable<IMetadataObject> Grep<TRepository>(IObjectRepositoryContainer<TRepository> container, string content)
            where TRepository : IObjectRepository;
    }
}
