using LibGit2Sharp;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitObjectDb.Backends
{
    public class RedisBackend : AbstractOdbBackend, IDisposable
    {
        readonly ConnectionMultiplexer _connection;
        readonly IDatabase _store;
        (ObjectId Id, StoreItem Item)? _lastItem;

        public RedisBackend(string host)
        {
            _connection = ConnectionMultiplexer.Connect("localhost");
            _store = _connection.GetDatabase();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        public override bool Exists(ObjectId id) => _store.KeyExistsAsync(id.Sha).Result;

        public override int Read(ObjectId id, out UnmanagedMemoryStream data, out ObjectType objectType)
        {
            var lastItem = _lastItem; // Thread safety
            var entry = lastItem.HasValue && lastItem.Value.Id.Equals(id) ?
                lastItem.Value.Item :
                JsonConvert.DeserializeObject<StoreItem>(_store.StringGetAsync(id.Sha).Result);
            objectType = entry.ObjectType;
            data = Allocate(entry.Data.LongLength);
            using (var reader = new MemoryStream(entry.Data))
            {
                reader.CopyTo(data);
            }
            return (int)ReturnCode.GIT_OK;
        }

        public override int Write(ObjectId id, Stream dataStream, long length, ObjectType objectType)
        {
            var value = new StoreItem
            {
                Data = ReadStream(dataStream, length),
                ObjectType = objectType
            };
            _store.StringSetAsync(id.Sha, JsonConvert.SerializeObject(value)).Wait();
            _lastItem = (id, value);
            return (int)ReturnCode.GIT_OK;
        }
    }
}
