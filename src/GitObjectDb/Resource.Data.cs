using GitObjectDb.Tools;
using System;
using System.IO;
using System.Text;

namespace GitObjectDb;

public sealed partial record Resource
{
    /// <summary>Single resource linked to a <see cref="Node"/>.</summary>
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public sealed class Data
    {
        private readonly Func<Stream> _stream;

        /// <summary>Initializes a new instance of the <see cref="Data"/> class.</summary>
        /// <param name="value">The resource content.</param>
        public Data(string value)
            : this(new StringReaderStream(value))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="Data"/> class.</summary>
        /// <param name="stream">The resource content.</param>
        public Data(Stream stream)
            : this(() => stream)
        {
        }

        internal Data(Func<Stream> value)
        {
            _stream = value;
        }

        /// <summary>Gets the content stream.</summary>
        /// <returns>The stream.</returns>
        public Stream GetContentStream() =>
            _stream.Invoke();

        /// <summary>Reads the resource stream as a string.</summary>
        /// <param name="encoding">The character encoding to use.</param>
        /// <returns>The string content of the stream.</returns>
        public string ReadAsString(Encoding? encoding = null)
        {
            using var reader = new StreamReader(GetContentStream(), encoding ?? Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
