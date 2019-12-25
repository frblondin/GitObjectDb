using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <param name="comparison">The type of comparison.</param>
        /// <returns>All objects containing the <paramref name="content"/> value.</returns>
        IAsyncEnumerable<IModelObject> GrepAsync(IObjectRepository repository, string content, CancellationToken cancellationToken, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Searches for the specified content in the repository.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="content">The content.</param>
        /// <param name="comparison">The type of comparison.</param>
        /// <returns>All objects containing the <paramref name="content"/> value.</returns>
        IAsyncEnumerable<IModelObject> GrepAsync(IObjectRepository repository, string content, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Searches for the specified content in the repository.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="content">The content.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <param name="comparison">The type of comparison.</param>
        /// <returns>All objects containing the <paramref name="content"/> value.</returns>
        IAsyncEnumerable<IModelObject> GrepAsync(IObjectRepositoryContainer container, string content, CancellationToken cancellationToken, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Searches for the specified content in the repository.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="content">The content.</param>
        /// <param name="comparison">The type of comparison.</param>
        /// <returns>All objects containing the <paramref name="content"/> value.</returns>
        IAsyncEnumerable<IModelObject> GrepAsync(IObjectRepositoryContainer container, string content, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the objects having a reference to this instance.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="node">The object.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>All objects having a link whose target is the <paramref name="node"/>.</returns>
        IAsyncEnumerable<IModelObject> GetReferrersAsync<TModel>(TModel node, CancellationToken cancellationToken)
            where TModel : class, IModelObject;

        /// <summary>
        /// Gets the objects having a reference to this instance.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="node">The object.</param>
        /// <returns>All objects having a link whose target is the <paramref name="node"/>.</returns>
        IAsyncEnumerable<IModelObject> GetReferrers<TModel>(TModel node)
            where TModel : class, IModelObject;
    }
}
