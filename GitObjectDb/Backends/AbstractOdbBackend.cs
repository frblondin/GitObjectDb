using LibGit2Sharp;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Backends
{
    public abstract partial class AbstractOdbBackend : OdbBackend
    {
        public class StoreItem
        {
            public byte[] Data { get; set; }
            public ObjectType ObjectType { get; set; }
        }

        protected override OdbBackendOperations SupportedOperations =>
            OdbBackendOperations.Read |
            OdbBackendOperations.Write |
            OdbBackendOperations.ReadPrefix |
            OdbBackendOperations.WriteStream |
            OdbBackendOperations.Exists |
            OdbBackendOperations.ExistsPrefix |
            OdbBackendOperations.ForEach;

        public override int ExistsPrefix(string shortSha, out ObjectId found) =>
            throw new NotImplementedException();

        public override int ForEach(ForEachCallback callback) =>
            throw new NotImplementedException();

        public override int ReadHeader(ObjectId id, out int length, out ObjectType objectType) =>
            throw new NotImplementedException();

        public override int ReadPrefix(string shortSha, out ObjectId oid, out UnmanagedMemoryStream data, out ObjectType objectType) =>
            throw new NotImplementedException();

        public override int ReadStream(ObjectId id, out OdbBackendStream stream) =>
            throw new NotImplementedException();

        public override int WriteStream(long length, ObjectType objectType, out OdbBackendStream stream)
        {
            stream = new WriteOnlyStream(this, objectType, length);

            return (int)ReturnCode.GIT_OK;
        }

        #region Utils
        protected static byte[] ReadStream(Stream stream, long length)
        {
            var result = (byte[])Array.CreateInstance(typeof(byte), length);
            stream.Read(result, 0, (int)length);
            return result;
        }
        #endregion
    }
}
