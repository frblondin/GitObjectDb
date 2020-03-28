using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Caching;
using System.Text;

namespace GitObjectDb.Serialization
{
    internal class NodeSerializerCache : INodeSerializer, IDisposable
    {
        private readonly MemoryCache _cache;
        private readonly INodeSerializer _serializer;

        public NodeSerializerCache(INodeSerializer serializer)
        {
            _cache = new MemoryCache(nameof(GitObjectDb));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public NonScalar Deserialize(Stream stream, DataPath path, string sha, Func<DataPath, ITreeItem> referenceResolver)
        {
            var result = _cache[sha] as NonScalar;
            if (result == null)
            {
                result = _serializer.Deserialize(stream, path, sha, referenceResolver);
                _cache.Set(sha, result, new CacheItemPolicy
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                });
            }
            return result;
        }

        public Stream Serialize(Node node) => _serializer.Serialize(node);

        public string BindToName(Type type) => _serializer.BindToName(type);

        public Type BindToType(string fullTypeName) => _serializer.BindToType(fullTypeName);

        public void Dispose() => _cache.Dispose();
    }
}
