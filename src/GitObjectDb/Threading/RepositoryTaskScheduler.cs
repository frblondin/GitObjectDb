using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Threading
{
    internal static class RepositoryTaskScheduler
    {
        public static TaskFactory Factory => Task.Factory;

        public static TaskScheduler Scheduler => TaskScheduler.Current ?? TaskScheduler.Default;

        /// <summary>
        /// Returns the result of the provided function processing.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="processor">A function delegate that returns the future result to be available through the <see cref="Task{TResult}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>The result of the function call.</returns>
        public static Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> processor, CancellationToken? cancellationToken = null) =>
            ExecuteAsync<Task<TResult>>(processor, cancellationToken).Unwrap();

        /// <summary>
        /// Returns the result of the provided function processing.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="processor">A function delegate that returns the future result to be available through the <see cref="Task{TResult}"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>The result of the function call.</returns>
        public static Task<TResult> ExecuteAsync<TResult>(Func<TResult> processor, CancellationToken? cancellationToken = null) =>
            RepositoryTaskScheduler.Factory.StartNew(
                processor,
                cancellationToken ?? CancellationToken.None,
                Factory.CreationOptions | TaskCreationOptions.DenyChildAttach,
                Scheduler);

        /// <summary>
        /// Calls the provided function processing.
        /// </summary>
        /// <param name="processor">The function.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that will be assigned to the new task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task ExecuteAsync(Action processor, CancellationToken? cancellationToken = null) =>
            ExecuteAsync<object>(() =>
            {
                processor();
                return null;
            }, cancellationToken);
    }
}
