using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace GitObjectDb.Tools;

internal class StringReaderStream : Stream
{
    private readonly int _maxBytesPerChar;

    private int _inputPosition;
    private long _position;

    public StringReaderStream(string input)
        : this(input, Encoding.UTF8)
    {
    }

    public StringReaderStream(string input, Encoding encoding)
    {
        Value = input;
        Encoding = encoding;
        Length = encoding.GetByteCount(input);
        _maxBytesPerChar = encoding.Equals(Encoding.ASCII) ? 1 : encoding.GetMaxByteCount(1);
    }

    public string Value { get; }

    public Encoding Encoding { get; }

    [ExcludeFromCodeCoverage]
    public override bool CanRead => true;

    [ExcludeFromCodeCoverage]
    public override bool CanSeek => false;

    [ExcludeFromCodeCoverage]
    public override bool CanWrite => false;

    [ExcludeFromCodeCoverage]
    public override long Length { get; }

    [ExcludeFromCodeCoverage]
    public override long Position
    {
        get => _position;
        set
        {
            if (value == 0)
            {
                Reset();
                return;
            }
            throw new NotSupportedException();
        }
    }

    [ExcludeFromCodeCoverage]
    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_inputPosition >= Value.Length)
        {
            return 0;
        }

        var charCount = count < _maxBytesPerChar ?
            1 :
            Math.Min(Value.Length - _inputPosition, count / _maxBytesPerChar);
        var byteCount = Encoding.GetBytes(Value, _inputPosition, charCount, buffer, offset);
        _inputPosition += charCount;
        _position += byteCount;
        return byteCount;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        if (offset == 0 && origin == SeekOrigin.Begin)
        {
            Reset();
            return 0L;
        }
        throw new NotSupportedException();
    }

    private void Reset()
    {
        _inputPosition = 0;
        _position = 0;
    }

    [ExcludeFromCodeCoverage]
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    [ExcludeFromCodeCoverage]
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
