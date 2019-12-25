using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Git
{
    /// <summary>
    /// Provides access to reusable repository instances that get automatically evicted
    /// after a timeout duration.
    /// </summary>
    public interface IRepositoryProvider
    {
        /// <summary>
        /// Returns the result of the provided function processing.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="description">The description.</param>
        /// <param name="processor">The function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>The result of the function call.</returns>
        Task<TResult> ExecuteAsync<TResult>(RepositoryDescription description, Func<IRepository, Task<TResult>> processor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the result of the provided function processing.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="description">The description.</param>
        /// <param name="processor">The function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>The result of the function call.</returns>
        Task<TResult> ExecuteAsync<TResult>(RepositoryDescription description, Func<IRepository, TResult> processor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the result of the provided function processing.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="description">The description.</param>
        /// <param name="processor">The function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>The result of the function call.</returns>
        IAsyncEnumerable<TResult> ExecuteAsyncEnumerator<TResult>(RepositoryDescription description, Func<IRepository, IAsyncEnumerable<TResult>> processor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calls the provided function processing.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="processor">The function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task ExecuteAsync(RepositoryDescription description, Action<IRepository> processor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Disposes the specified description.
        /// </summary>
        /// <param name="description">The description.</param>
        void Evict(RepositoryDescription description);
    }
}