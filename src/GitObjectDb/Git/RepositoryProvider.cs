using GitObjectDb.Threading;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Git
{
    /// <inheritdoc/>
    internal sealed class RepositoryProvider : IRepositoryProvider, IDisposable
    {
        private static readonly TimeSpan _expirationScanFrequency = TimeSpan.FromSeconds(2);

        private readonly object _syncLock = new object();
        private readonly IDictionary<RepositoryDescription, CacheEntry> _dictionary = new Dictionary<RepositoryDescription, CacheEntry>();
        private readonly IRepositoryFactory _repositoryFactory;

        private CancellationTokenSource _scanForExpiredItemsCancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryProvider"/> class.
        /// </summary>
        /// <param name="repositoryFactory">The repository factory.</param>
        public RepositoryProvider(IRepositoryFactory repositoryFactory)
        {
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<TResult> ExecuteAsyncEnumerator<TResult>(RepositoryDescription description, Func<IRepository, IAsyncEnumerable<TResult>> processor, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (processor is null)
            {
                throw new ArgumentNullException(nameof(processor));
            }

            var entry = GetEntry(description);
            try
            {
                await foreach (var item in await RepositoryTaskScheduler.Factory.StartNew(() => processor(entry.Repository), cancellationToken, TaskCreationOptions.None, RepositoryTaskScheduler.Scheduler).ConfigureAwait(false))
                {
                    yield return item;
                }
            }
            finally
            {
                lock (_syncLock)
                {
                    entry.Counter--;
                }
                StartScanForExpiredItems();
            }
        }

        /// <inheritdoc/>
        public Task<TResult> ExecuteAsync<TResult>(RepositoryDescription description, Func<IRepository, TResult> processor, CancellationToken cancellationToken = default)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }

            var entry = GetEntry(description);
            return RepositoryTaskScheduler.ExecuteAsync(() => processor(entry.Repository), cancellationToken)
                .ContinueWith((Task<TResult> task) =>
                {
                    lock (_syncLock)
                    {
                        entry.Counter--;
                    }
                    StartScanForExpiredItems();
                    return WaitAndUnwrapException(task);
                },
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                RepositoryTaskScheduler.Scheduler);
        }

        private static TResult WaitAndUnwrapException<TResult>(Task<TResult> task) => task.GetAwaiter().GetResult();

        /// <inheritdoc/>
        public Task<TResult> ExecuteAsync<TResult>(RepositoryDescription description, Func<IRepository, Task<TResult>> processor, CancellationToken cancellationToken = default)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }

            var entry = GetEntry(description);
            return RepositoryTaskScheduler.ExecuteAsync<Task<TResult>>(() => processor(entry.Repository), cancellationToken)
                .Unwrap()
                .ContinueWith((Task<TResult> task) =>
                {
                    lock (_syncLock)
                    {
                        entry.Counter--;
                    }
                    StartScanForExpiredItems();
                    return WaitAndUnwrapException(task);
                },
                cancellationToken,
                TaskContinuationOptions.ExecuteSynchronously,
                RepositoryTaskScheduler.Scheduler);
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync(RepositoryDescription description, Action<IRepository> processor, CancellationToken cancellationToken = default)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }

            await ExecuteAsync<object>(description, repository =>
            {
                processor(repository);
                return null;
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public void Evict(RepositoryDescription description)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            lock (_syncLock)
            {
                var kvp = _dictionary.FirstOrDefault(k => k.Key.Equals(description));
                if (kvp.Key != null)
                {
                    Evict(kvp);
                }
            }
        }

        private CacheEntry GetEntry(RepositoryDescription description)
        {
            lock (_syncLock)
            {
                if (!_dictionary.TryGetValue(description, out var result))
                {
                    _dictionary[description] = result = new CacheEntry(_repositoryFactory.CreateRepository(description));
                }
                result.Counter++;
                result.LastUsed = DateTimeOffset.UtcNow;
                return result;
            }
        }

        private void StartScanForExpiredItems()
        {
            _scanForExpiredItemsCancellationTokenSource?.Cancel();
            _scanForExpiredItemsCancellationTokenSource = new CancellationTokenSource();

            var delay = Task.Delay(_expirationScanFrequency, _scanForExpiredItemsCancellationTokenSource.Token);
            delay.ContinueWith(ScanForExpiredItems, CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private void ScanForExpiredItems(Task task)
        {
            lock (_syncLock)
            {
                var expiredItems = (from kvp in _dictionary
                                    where kvp.Value.Counter == 0 && kvp.Value.ShouldBeEvicted
                                    select kvp).ToList();
                foreach (var kvp in expiredItems)
                {
                    Evict(kvp);
                }
            }
        }

        private void Evict(KeyValuePair<RepositoryDescription, CacheEntry> kvp)
        {
            var collection = (ICollection<KeyValuePair<RepositoryDescription, CacheEntry>>)_dictionary;

            // By using the ICollection<KVP>.Remove overload, we additionally enforce that the exact value in the KVP is the one associated with the key.
            // this would prevent, for instance, removing an item if it got touched right after we enumerated it above.
            collection.Remove(kvp);
            kvp.Value.Evict();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _scanForExpiredItemsCancellationTokenSource?.Dispose();
        }

        private class CacheEntry
        {
            private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);

            public CacheEntry(IRepository repository)
            {
                Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            }

            public IRepository Repository { get; }

            public int Counter { get; internal set; }

            public DateTimeOffset LastUsed { get; internal set; }

            public bool ShouldBeEvicted => Counter == 0 && (DateTimeOffset.UtcNow - LastUsed) > _timeout;

            public void Evict() => Repository.Dispose();
        }
    }
}