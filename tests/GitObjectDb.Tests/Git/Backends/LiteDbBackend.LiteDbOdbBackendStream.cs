using LibGit2Sharp;
using LiteDB;
using System;
using System.IO;
using System.Text;

namespace GitObjectDb.Tests.Git.Backends
{
    public sealed partial class LiteDbBackend
    {
        private class LiteDbOdbBackendStream : OdbBackendStream, IDisposable
        {
            private readonly ObjectType _objectType;
            private readonly long _length;

            private readonly Stream _stream;

            public LiteDbOdbBackendStream(LiteDbBackend backend, Stream stream)
                : base(backend)
            {
                _stream = stream;
            }

            public LiteDbOdbBackendStream(LiteDbBackend backend, ObjectType objectType, long length)
                : base(backend)
            {
                _objectType = objectType;
                _length = length;
                _stream = new MemoryStream();
            }

            public override bool CanRead => true;

            public override bool CanWrite => true;

            public override int Read(Stream dataStream, long length)
            {
                var bytes = new byte[(int)length];
                var readLength = _stream.Read(bytes, 0, (int)length);
                dataStream.Write(bytes, 0, readLength);
                return (int)ReturnCode.GIT_OK;
            }

            public override int Write(Stream dataStream, long length)
            {
                dataStream.CopyTo(_stream, (int)length);
                return (int)ReturnCode.GIT_OK;
            }

            public override int FinalizeWrite(LibGit2Sharp.ObjectId id)
            {
                _stream.Seek(0L, SeekOrigin.Begin);
                return Backend.Write(id, _stream, _length, _objectType);
            }

            void IDisposable.Dispose()
            {
                _stream.Dispose();
            }

            protected override void Dispose()
            {
                _stream.Dispose();
                base.Dispose();
            }
        }
    }
}