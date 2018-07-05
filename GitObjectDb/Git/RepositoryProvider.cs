using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Git
{
    /// <inheritdoc/>
    internal sealed class RepositoryProvider : IRepositoryProvider
    {
        static readonly TimeSpan _expirationScanFrequency = TimeSpan.FromSeconds(2);

        readonly object _syncLock = new object();
        readonly IDictionary<RepositoryDescription, CacheEntry> _dictionary = new Dictionary<RepositoryDescription, CacheEntry>();
        readonly IRepositoryFactory _repositoryFactory;

        DateTimeOffset _lastExpirationScan = DateTimeOffset.UtcNow;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryProvider"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <exception cref="ArgumentNullException">
        /// memoryCache
        /// or
        /// repositoryFactory
        /// </exception>
        public RepositoryProvider(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _repositoryFactory = serviceProvider.GetRequiredService<IRepositoryFactory>();
        }

        /// <inheritdoc/>
        public TResult Execute<TResult>(RepositoryDescription description, Func<IRepository, TResult> function)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            var entry = GetEntry(description);
            try
            {
                return function(entry.Repository);
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

        CacheEntry GetEntry(RepositoryDescription description)
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

        void StartScanForExpiredItems()
        {
            var utcNow = DateTimeOffset.UtcNow;
            if (utcNow - _lastExpirationScan > _expirationScanFrequency)
            {
                _lastExpirationScan = utcNow;
                Task.Factory.StartNew(
                    state => ScanForExpiredItems(),
                    this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        void ScanForExpiredItems()
        {
            var expiredItems = (from kvp in _dictionary
                                where kvp.Value.Counter == 0 && kvp.Value.ShouldBeEvicted
                                select kvp).ToList();
            var collection = (ICollection<KeyValuePair<RepositoryDescription, CacheEntry>>)_dictionary;
            foreach (var kvp in expiredItems)
            {
                // By using the ICollection<KVP>.Remove overload, we additionally enforce that the exact value in the KVP is the one associated with the key.
                // this would prevent, for instance, removing an item if it got touched right after we enumerated it above.
                collection.Remove(kvp);
                kvp.Value.Evict();
            }
        }

        class CacheEntry
        {
            static readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);

            public int Counter;
            public DateTimeOffset LastUsed;

            public CacheEntry(IRepository repository)
            {
                Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            }

            public IRepository Repository { get; }

            public bool ShouldBeEvicted => Counter == 0 && (DateTimeOffset.UtcNow - LastUsed) > _timeout;

            public void Evict() => Repository.Dispose();
        }
    }
}