using System;
using System.IO;
using System.Text;

namespace GitObjectDb
{
    public sealed partial class Resource
    {
        private class StringReaderStream : Stream
        {
            private readonly Encoding _encoding;
            private readonly string _input;
            private readonly int _maxBytesPerChar;

            private int _inputPosition;
            private long _position;

            public StringReaderStream(string input)
                : this(input, Encoding.Default)
            {
            }

            public StringReaderStream(string input, Encoding encoding)
            {
                _input = input;
                _encoding = encoding;
                Length = encoding.GetByteCount(input);
                _maxBytesPerChar = encoding == Encoding.ASCII ? 1 : encoding.GetMaxByteCount(1);
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length { get; }

            public override long Position
            {
                get => _position;
                set
                {
                    if (value == 0)
                    {
                        Reset();
                    }
                    throw new NotImplementedException();
                }
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_inputPosition >= _input.Length)
                {
                    return 0;
                }

                if (count < _maxBytesPerChar)
                {
                    throw new ArgumentException($"{nameof(count)} has to be greater or equal to max encoding byte count per char.");
                }

                var charCount = Math.Min(_input.Length - _inputPosition, count / _maxBytesPerChar);
                var byteCount = _encoding.GetBytes(_input, _inputPosition, charCount, buffer, offset);
                _inputPosition += charCount;
                _position += byteCount;
                return byteCount;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (offset == 0 && origin == SeekOrigin.Begin)
                {
                    Reset();
                }
                throw new NotImplementedException();
            }

            private void Reset()
            {
                _inputPosition = 0;
                _position = 0;
            }

            public override void SetLength(long value) => throw new NotImplementedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }
    }
}
