using System;
using System.Collections.Concurrent;

namespace GitObjectDb.Reflection
{
    /// <inheritdoc />
    internal class CachedModelDataAccessorProvider : IModelDataAccessorProvider
    {
        private readonly IModelDataAccessorProvider _inner;
        private readonly ConcurrentDictionary<Type, IModelDataAccessor> _cache = new ConcurrentDictionary<Type, IModelDataAccessor>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CachedModelDataAccessorProvider"/> class.
        /// </summary>
        /// <param name="inner">The inner.</param>
        public CachedModelDataAccessorProvider(IModelDataAccessorProvider inner) =>
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        /// <inheritdoc />
        public IModelDataAccessor Get(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _cache.GetOrAdd(type, _inner.Get);
        }
    }
}
