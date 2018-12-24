using GitObjectDb.Attributes;
using LibGit2Sharp;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GitObjectDb.Tests.Git.Backends
{
    /// <summary>
    /// Backend storing blobs in a Redis database.
    /// </summary>
    [ExcludeFromGuardForNull]
    public sealed partial class LiteDbBackend : OdbBackend, IDisposable
    {
        private readonly LiteDatabase _database;
        private StoreItem _lastItem;

        /// <inheritdoc />
        protected override OdbBackendOperations SupportedOperations =>
            OdbBackendOperations.Read |
            OdbBackendOperations.Write |
            OdbBackendOperations.ReadPrefix |
            OdbBackendOperations.WriteStream |
            OdbBackendOperations.Exists |
            OdbBackendOperations.ExistsPrefix |
            OdbBackendOperations.ForEach;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbBackend" /> class.
        /// </summary>
        /// <param name="host">The host DNS/IP address.</param>
        public LiteDbBackend(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            _database = new LiteDatabase(connectionString);
        }

        /// <inheritdoc />
        public override bool Exists(LibGit2Sharp.ObjectId id) =>
            (_lastItem?.Sha.Equals(id.Sha, StringComparison.OrdinalIgnoreCase) ?? false) ||
            _database.FileStorage.Exists(id.Sha);

        /// <inheritdoc />
        public override int Write(LibGit2Sharp.ObjectId id, Stream dataStream, long length, ObjectType objectType)
        {
            var data = (byte[])Array.CreateInstance(typeof(byte), length);
            dataStream.Read(data, 0, (int)length);

            using (var stream = _database.FileStorage.OpenWrite(id.Sha, null))
            {
                WriteData(id, objectType, data, stream);

                _lastItem = new StoreItem(id.Sha, objectType, data);
            }
            return (int)ReturnCode.GIT_OK;
        }

        /// <inheritdoc />
        public override int Read(LibGit2Sharp.ObjectId id, out UnmanagedMemoryStream data, out ObjectType objectType)
        {
            var lastItem = _lastItem; // Thread safety
            if (lastItem?.Sha.Equals(id.Sha, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                data = AllocateAndBuildFrom(lastItem.Data);
                objectType = lastItem.ObjectType;
                return (int)ReturnCode.GIT_OK;
            }
            var entry = _database.FileStorage.FindById(id.Sha);
            if (entry != null)
            {
                ExtractData(entry, out var _, out data, out objectType);
                return (int)ReturnCode.GIT_OK;
            }
            else
            {
                data = default;
                objectType = default;
                return (int)ReturnCode.GIT_ENOTFOUND;
            }
        }

        /// <inheritdoc />
        public override int ReadPrefix(string shortSha, out LibGit2Sharp.ObjectId oid, out UnmanagedMemoryStream data, out ObjectType objectType)
        {
            var lastItem = _lastItem; // Thread safety
            if (lastItem?.Sha.StartsWith(shortSha, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                oid = new LibGit2Sharp.ObjectId(lastItem.Sha);
                data = AllocateAndBuildFrom(lastItem.Data);
                objectType = lastItem.ObjectType;
                return (int)ReturnCode.GIT_OK;
            }

            var entries = _database.FileStorage.Find(shortSha).ToList();
            if (entries.Count == 1)
            {
                ExtractData(entries[0], out oid, out data, out objectType);
                return (int)ReturnCode.GIT_OK;
            }
            else
            {
                oid = default;
                data = default;
                objectType = default;
                return (int)(entries.Count == 0 ? ReturnCode.GIT_ENOTFOUND : ReturnCode.GIT_EAMBIGUOUS);
            }
        }

        /// <inheritdoc />
        public override int ReadHeader(LibGit2Sharp.ObjectId id, out int length, out ObjectType objectType)
        {
            var lastItem = _lastItem; // Thread safety
            if (lastItem?.Sha.Equals(id.Sha, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                length = lastItem.Data.Length;
                objectType = lastItem.ObjectType;
                return (int)ReturnCode.GIT_OK;
            }
            var entry = _database.FileStorage.FindById(id.Sha);
            if (entry != null)
            {
                ExtractData(entry, out var _, out length, out objectType);
                return (int)ReturnCode.GIT_OK;
            }
            else
            {
                length = default;
                objectType = default;
                return (int)ReturnCode.GIT_ENOTFOUND;
            }
        }

        /// <inheritdoc />
        public override int ReadStream(LibGit2Sharp.ObjectId id, out OdbBackendStream stream)
        {
            var lastItem = _lastItem; // Thread safety
            if (lastItem?.Sha.Equals(id.Sha, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                stream = new LiteDbOdbBackendStream(this, new MemoryStream(lastItem.Data));
                return (int)ReturnCode.GIT_OK;
            }
            var entry = _database.FileStorage.FindById(id.Sha);
            if (entry != null)
            {
                stream = new LiteDbOdbBackendStream(this, entry.OpenRead());
                return (int)ReturnCode.GIT_OK;
            }
            else
            {
                stream = default;
                return (int)ReturnCode.GIT_ENOTFOUND;
            }
        }

        /// <inheritdoc />
        public override int WriteStream(long length, ObjectType objectType, out OdbBackendStream stream)
        {
            stream = new LiteDbOdbBackendStream(this, objectType, length);
            return (int)ReturnCode.GIT_OK;
        }

        /// <inheritdoc />
        public override int ExistsPrefix(string shortSha, out LibGit2Sharp.ObjectId found)
        {
            var lastItem = _lastItem; // Thread safety
            if (lastItem?.Sha.StartsWith(shortSha, StringComparison.OrdinalIgnoreCase) ?? false)
            {
                found = new LibGit2Sharp.ObjectId(lastItem.Sha);
                return (int)ReturnCode.GIT_OK;
            }

            var entries = _database.FileStorage.Find(shortSha).ToList();
            if (entries.Count == 1)
            {
                ExtractData(entries[0], out found, out int _, out var __);
                return (int)ReturnCode.GIT_OK;
            }
            else
            {
                found = default;
                return (int)(entries.Count == 0 ? ReturnCode.GIT_ENOTFOUND : ReturnCode.GIT_EAMBIGUOUS);
            }
        }

        /// <inheritdoc />
        public override int ForEach(ForEachCallback callback)
        {
            foreach (var entry in _database.FileStorage.FindAll())
            {
                try
                {
                    ExtractData(entry, out var oid, out int _, out var __);
                    callback(oid);
                }
                catch
                {
                }
            }
            return (int)ReturnCode.GIT_OK;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _database?.Dispose();
        }

        private static void WriteData(LibGit2Sharp.ObjectId id, ObjectType objectType, byte[] data, LiteFileStream stream)
        {
            using (var writer = new BinaryWriter(stream, Encoding.Default, leaveOpen: true))
            {
                writer.Write(id.Sha);
                writer.Write((int)objectType);
                writer.Write(data.Length);
                writer.Write(data);
            }
        }

        private void ExtractData(LiteFileInfo file, out LibGit2Sharp.ObjectId oid, out UnmanagedMemoryStream data, out ObjectType objectType)
        {
            using (var reader = new BinaryReader(file.OpenRead()))
            {
                oid = new LibGit2Sharp.ObjectId(reader.ReadString());
                objectType = (ObjectType)reader.ReadInt32();
                var length = reader.ReadInt32();
                var bytes = reader.ReadBytes(length);
                data = AllocateAndBuildFrom(bytes);

                // Update last item
                _lastItem = new StoreItem(oid.Sha, objectType, bytes);
            }
        }

        private static void ExtractData(LiteFileInfo file, out LibGit2Sharp.ObjectId oid, out int length, out ObjectType objectType)
        {
            using (var reader = new BinaryReader(file.OpenRead()))
            {
                oid = new LibGit2Sharp.ObjectId(reader.ReadString());
                objectType = (ObjectType)reader.ReadInt32();
                length = reader.ReadInt32();
            }
        }
    }
}