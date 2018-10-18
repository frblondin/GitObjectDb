using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitObjectDb.Tests.Git.Backends
{
    public abstract partial class AbstractOdbBackend
    {
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
                }
                while (bytesRead != 0);

                if (offset != (int)length)
                {
                    throw new InvalidOperationException($"Too short buffer. {length} bytes were expected. {bytesRead} have been successfully read.");
                }

                _chunks.Add(buffer);

                return (int)ReturnCode.GIT_OK;
            }

            public override int FinalizeWrite(ObjectId id)
            {
                // TODO: Drop the check of the size when libgit2 #1837 is merged
                long totalLength = _chunks.Sum(chunk => chunk.Length);

                if (totalLength != _length)
                {
                    throw new InvalidOperationException(
                        $"Invalid object length. {_length} was expected. The total size of the received chunks amounts to {totalLength}.");
                }

                using (Stream stream = new FakeStream(_chunks, _length))
                {
                    Backend.Write(id, stream, _length, _type);
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
                private int _currentChunk = 0;
                private int _currentPos = 0;

                public FakeStream(IList<byte[]> chunks, long length)
                {
                    _chunks = chunks;
                    _length = length;
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
                        if (_currentChunk > _chunks.Count - 1)
                        {
                            return totalCopied;
                        }

                        var toBeCopied = Math.Min(_chunks[_currentChunk].Length - _currentPos, count - totalCopied);

                        Buffer.BlockCopy(_chunks[_currentChunk], _currentPos, buffer, offset + totalCopied, toBeCopied);
                        _currentPos += toBeCopied;
                        totalCopied += toBeCopied;

                        Debug.Assert(_currentPos <= _chunks[_currentChunk].Length, "Unexpected position in current chunk.");

                        if (_currentPos == _chunks[_currentChunk].Length)
                        {
                            _currentPos = 0;
                            _currentChunk++;
                        }
                    }

                    return totalCopied;
                }

                public override void Write(byte[] buffer, int offset, int count)
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}