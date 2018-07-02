using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitObjectDb.Git.Backends
{
    /// <summary>
    /// Testing backend storing all blobs to an in-memory dictionary.
    /// </summary>
    public class InMemoryBackend : AbstractOdbBackend
    {
        private readonly IDictionary<ObjectId, StoreItem> _store = new Dictionary<ObjectId, StoreItem>();
        private (ObjectId Id, StoreItem Item)? _lastItem;

        /// <inheritdoc />
        public override bool Exists(ObjectId id) => _store.ContainsKey(id);

        /// <inheritdoc />
        public override int Read(ObjectId id, out UnmanagedMemoryStream data, out ObjectType objectType)
        {
            var lastItem = _lastItem; // Thread safety
            var entry = lastItem.HasValue && lastItem.Value.Id.Equals(id) ? lastItem.Value.Item : _store[id];
            objectType = entry.ObjectType;
            data = Allocate(entry.Data.LongLength);
            using (var reader = new MemoryStream(entry.Data))
            {
                reader.CopyTo(data);
            }
            return (int)ReturnCode.GIT_OK;
        }

        /// <inheritdoc />
        public override int Write(ObjectId id, Stream dataStream, long length, ObjectType objectType)
        {
            var value = new StoreItem
            {
                Data = ReadStream(dataStream, length),
                ObjectType = objectType
            };
            _store[id] = value;
            _lastItem = (id, value);
            return (int)ReturnCode.GIT_OK;
        }
    }
}