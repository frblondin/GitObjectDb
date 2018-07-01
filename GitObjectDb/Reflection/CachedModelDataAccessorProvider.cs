using System;
using System.Collections.Concurrent;

namespace GitObjectDb.Reflection
{
    /// <inheritdoc />
    internal class CachedModelDataAccessorProvider : IModelDataAccessorProvider
    {
        readonly IModelDataAccessorProvider _inner;
        readonly ConcurrentDictionary<Type, IModelDataAccessor> _cache = new ConcurrentDictionary<Type, IModelDataAccessor>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedModelDataAccessorProvider"/> class.
        /// </summary>
        /// <param name="inner">The inner.</param>
        /// <exception cref="ArgumentNullException">inner</exception>
        public CachedModelDataAccessorProvider(IModelDataAccessorProvider inner) =>
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        /// <inheritdoc />
        public IModelDataAccessor Get(Type type) => _cache.GetOrAdd(type, _inner.Get);
    }
}
