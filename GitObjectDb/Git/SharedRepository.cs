using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GitObjectDb.Git
{
    /// <summary>
    /// This is a workaround to corrupted memory issues occuring when multiple repositories
    /// are used.
    /// </summary>
    /// <seealso cref="IDisposable" />
    internal sealed class SharedRepository : IDisposable
    {
        static readonly ThreadLocal<SharedRepository> _instance = new ThreadLocal<SharedRepository>();
        readonly bool _dispose;

        SharedRepository(IRepository repository, bool dispose = true)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _dispose = dispose;
        }

        /// <summary>
        /// Gets the current repository if any.
        /// </summary>
        internal static IRepository Current => _instance.Value?.Repository;

        /// <summary>
        /// Gets the repository.
        /// </summary>
        public IRepository Repository { get; }

        /// <summary>
        /// Starts a new shared repository.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="dispose">if set to <c>true</c> [dispose].</param>
        /// <returns>The shared repository.</returns>
        /// <exception cref="NotSupportedException">Nested shared repositories are not supported.</exception>
        internal static IRepository Start(Func<IRepository> repository, bool dispose = true)
        {
            if (_instance.Value != null)
            {
                throw new NotSupportedException("Nested shared repositories are not supported.");
            }
            var result = new SharedRepository(repository(), dispose);
            _instance.Value = result;
            return result.Repository;
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            if (_dispose)
            {
                Repository.Dispose();
            }
            _instance.Value = null;
        }
    }
}