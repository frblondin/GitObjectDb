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
        IEnumerable<IModelObject> Grep(IObjectRepository repository, string content);

        /// <summary>
        /// Searches for the specified content in the repository.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="content">The content.</param>
        /// <returns>All objects containing the <paramref name="content"/> value.</returns>
        IEnumerable<IModelObject> Grep(IObjectRepositoryContainer container, string content);

        /// <summary>
        /// Gets the objects having a reference to this instance.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="node">The object.</param>
        /// <returns>All objects having a link whose target is the <paramref name="node"/>.</returns>
        IEnumerable<IModelObject> GetReferrers<TModel>(TModel node)
            where TModel : class, IModelObject;
    }
}
