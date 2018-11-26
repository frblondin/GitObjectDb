using GitObjectDb.Attributes;
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
    /// <summary>
    /// Creates an in-memory stream out of a <see cref="StringBuilder"/> instance.
    /// </summary>
    /// <seealso cref="MemoryStream" />
    [ExcludeFromGuardForNull]
    internal class StringBuilderStream : MemoryStream
    {
        private readonly long _length;
        private int _positionInStringBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuilderStream"/> class.
        /// </summary>
        /// <param name="stringBuilder">The string builder.</param>
        /// <param name="encoding">The encoding.</param>
        /// <exception cref="ArgumentNullException">stringBuilder</exception>
        public StringBuilderStream(StringBuilder stringBuilder, Encoding encoding = null)
        {
            StringBuilder = stringBuilder ?? throw new ArgumentNullException(nameof(stringBuilder));
            Encoding = encoding ?? Encoding.Default;

            _length = ComputeLength();
        }

        /// <summary>
        /// Gets the string builder.
        /// </summary>
        public StringBuilder StringBuilder { get; }

        /// <summary>
        /// Gets the encoding.
        /// </summary>
        public Encoding Encoding { get; }

        /// <inheritdoc />
        public override long Length => _length;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => false;

        private long ComputeLength() =>
            StringBuilder.GetSlices().Sum(s => Encoding.GetByteCount(s._values, s._indexInChunk, s._count));

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var result = StringBuilder.CopyTo(buffer, offset, count, ref _positionInStringBuilder, Length - Position, Encoding);
            Position += result;
            return result;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();

        /// <inheritdoc />
        public override int ReadByte() => throw new NotSupportedException();

        /// <inheritdoc />
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();

        /// <inheritdoc />
        public override int EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void WriteByte(byte value) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void WriteTo(Stream stream) => throw new NotSupportedException();

        /// <inheritdoc />
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();
    }
}
