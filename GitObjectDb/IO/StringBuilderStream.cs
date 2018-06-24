using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.IO
{
    internal class StringBuilderStream : MemoryStream
    {
        public StringBuilder StringBuilder { get; }
        public Encoding Encoding { get; }

        int _positionInStringBuilder;
        readonly long _length;
        public override long Length => _length;

        public StringBuilderStream(StringBuilder stringBuilder, Encoding encoding = null)
        {
            StringBuilder = stringBuilder ?? throw new ArgumentNullException(nameof(stringBuilder));
            Encoding = encoding ?? Encoding.Default;

            _length = ComputeLength();
        }

        long ComputeLength() =>
            StringBuilder.GetSlices().Sum(s => Encoding.GetByteCount(s.Values, s.IndexInChunk, s.Count));

        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = StringBuilder.CopyTo(buffer, offset, count, ref _positionInStringBuilder, Length - Position, Encoding);
            Position += result;
            return result;
        }

        public override bool CanSeek => false;
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();

        public override int ReadByte() => throw new NotSupportedException();
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();
        public override int EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new NotSupportedException();

        public override bool CanWrite => false;
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();
        public override void WriteByte(byte value) => throw new NotSupportedException();
        public override void WriteTo(Stream stream) => throw new NotSupportedException();
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();
        public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();
    }
}
