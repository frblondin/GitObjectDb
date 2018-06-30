using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Reflection
{
    public interface IModelDataAccessorProvider
    {
        IModelDataAccessor Get(Type type);
    }

    internal class ModelDataAccessorProvider : IModelDataAccessorProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public ModelDataAccessorProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IModelDataAccessor Get(Type type) => new ModelDataAccessor(_serviceProvider, type);
    }

    internal class CachedModelDataAccessorProvider : IModelDataAccessorProvider
    {
        readonly IModelDataAccessorProvider _inner;
        readonly ConcurrentDictionary<Type, IModelDataAccessor> _cache = new ConcurrentDictionary<Type, IModelDataAccessor>();

        public CachedModelDataAccessorProvider(IModelDataAccessorProvider inner) =>
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        public IModelDataAccessor Get(Type type) => _cache.GetOrAdd(type, _inner.Get);
    }
}
