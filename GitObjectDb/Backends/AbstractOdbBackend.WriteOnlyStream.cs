using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitObjectDb.Backends
{
    public abstract partial class AbstractOdbBackend
    {
        #region WriteOnlyStream
        private class WriteOnlyStream : OdbBackendStream
        {
            private readonly List<byte[]> _chunks = new List<byte[]>();

            private readonly ObjectType _type;
            private readonly long _length;

            public WriteOnlyStream(AbstractOdbBackend backend, ObjectType objectType, long length)
                : base(backend)
            {
                _type = objectType;
                _length = length;
            }

            public override bool CanRead => false;
            public override bool CanWrite => true;

            public override int Write(Stream dataStream, long length)
            {
                var buffer = new byte[length];

                int offset = 0, bytesRead;
                int toRead = Convert.ToInt32(length);

                do
                {
                    toRead -= offset;
                    bytesRead = dataStream.Read(buffer, offset, toRead);
                    offset += bytesRead;
                } while (bytesRead != 0);

                if (offset != (int)length)
                {
                    throw new InvalidOperationException(
                        string.Format("Too short buffer. {0} bytes were expected. {1} have been successfully read.",
                            length, bytesRead));
                }

                _chunks.Add(buffer);

                return (int)ReturnCode.GIT_OK;
            }

            public override int FinalizeWrite(ObjectId oid)
            {
                //TODO: Drop the check of the size when libgit2 #1837 is merged
                long totalLength = _chunks.Sum(chunk => chunk.Length);

                if (totalLength != _length)
                {
                    throw new InvalidOperationException(
                        string.Format("Invalid object length. {0} was expected. The "
                                      + "total size of the received chunks amounts to {1}.",
                                      _length, totalLength));
                }

                using (Stream stream = new FakeStream(_chunks, _length))
                {
                    Backend.Write(oid, stream, _length, _type);
                }

                return (int)ReturnCode.GIT_OK;
            }

            public override int Read(Stream dataStream, long length)
            {
                throw new NotImplementedException();
            }

            private class FakeStream : Stream
            {
                private readonly IList<byte[]> _chunks;
                private readonly long _length;
                private int currentChunk = 0;
                private int currentPos = 0;

                public FakeStream(IList<byte[]> chunks, long length)
                {
                    _chunks = chunks;
                    _length = length;
                }

                public override void Flush()
                {
                    throw new NotImplementedException();
                }

                public override long Seek(long offset, SeekOrigin origin)
                {
                    throw new NotImplementedException();
                }

                public override void SetLength(long value)
                {
                    throw new NotImplementedException();
                }

                public override int Read(byte[] buffer, int offset, int count)
                {
                    var totalCopied = 0;

                    while (totalCopied < count)
                    {
                        if (currentChunk > _chunks.Count - 1)
                        {
                            return totalCopied;
                        }

                        var toBeCopied = Math.Min(_chunks[currentChunk].Length - currentPos, count - totalCopied);

                        Buffer.BlockCopy(_chunks[currentChunk], currentPos, buffer, offset + totalCopied, toBeCopied);
                        currentPos += toBeCopied;
                        totalCopied += toBeCopied;

                        Debug.Assert(currentPos <= _chunks[currentChunk].Length);

                        if (currentPos == _chunks[currentChunk].Length)
                        {
                            currentPos = 0;
                            currentChunk++;
                        }
                    }

                    return totalCopied;
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    throw new NotImplementedException();
                }

                public override bool CanRead => true;
                public override bool CanSeek => throw new NotImplementedException();
                public override bool CanWrite => throw new NotImplementedException();

                public override long Length => _length;

                public override long Position
                {
                    get { throw new NotImplementedException(); }
                    set { throw new NotImplementedException(); }
                }
            }
        }
        #endregion
    }
}
