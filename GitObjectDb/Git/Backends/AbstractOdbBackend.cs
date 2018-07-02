using LibGit2Sharp;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Git.Backends
{
    /// <summary>
    /// Base abstract backend using to stream content to/from a custom storage system.
    /// </summary>
    public abstract partial class AbstractOdbBackend : OdbBackend
    {
        /// <inheritdoc />
        protected override OdbBackendOperations SupportedOperations =>
            OdbBackendOperations.Read |
            OdbBackendOperations.Write |
            OdbBackendOperations.ReadPrefix |
            OdbBackendOperations.WriteStream |
            OdbBackendOperations.Exists |
            OdbBackendOperations.ExistsPrefix |
            OdbBackendOperations.ForEach;

        /// <inheritdoc />
        public override int ExistsPrefix(string shortSha, out ObjectId found) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public override int ForEach(ForEachCallback callback) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public override int ReadHeader(ObjectId id, out int length, out ObjectType objectType) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public override int ReadPrefix(string shortSha, out ObjectId oid, out UnmanagedMemoryStream data, out ObjectType objectType) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public override int ReadStream(ObjectId id, out OdbBackendStream stream) =>
            throw new NotImplementedException();

        /// <inheritdoc />
        public override int WriteStream(long length, ObjectType objectType, out OdbBackendStream stream)
        {
            stream = new WriteOnlyStream(this, objectType, length);

            return (int)ReturnCode.GIT_OK;
        }

#pragma warning disable SA1600 // Elements must be documented

        #region Utils

        internal static byte[] ReadStream(Stream stream, long length)
        {
            var result = (byte[])Array.CreateInstance(typeof(byte), length);
            stream.Read(result, 0, (int)length);
            return result;
        }

        #endregion Utils

        internal class StoreItem
        {
            public byte[] Data { get; set; }

            public ObjectType ObjectType { get; set; }
        }

#pragma warning restore SA1600 // Elements must be documented
    }
}