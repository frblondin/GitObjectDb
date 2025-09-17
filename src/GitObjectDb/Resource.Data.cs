using GitObjectDb.Tools;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb;

public sealed partial record Resource
{
    /// <summary>Single resource linked to a <see cref="Node"/>.</summary>
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public sealed class Data
    {
        private readonly Func<Task<Stream>> _stream;

        /// <summary>Initializes a new instance of the <see cref="Data"/> class.</summary>
        /// <param name="value">The resource content.</param>
        public Data(string value)
            : this(new StringReaderStream(value))
        {
        }

        /// <summary>Initializes a new instance of the <see cref="Data"/> class.</summary>
        /// <param name="stream">The resource content.</param>
        public Data(Stream stream)
            : this(() => Task.FromResult(stream))
        {
        }

        internal Data(Func<Task<Stream>> value)
        {
            _stream = value;
        }

        /// <summary>Gets the content stream.</summary>
        /// <returns>The stream.</returns>
        public async Task<Stream> GetContentStreamAsync()
        {
            var result = await _stream.Invoke().ConfigureAwait(false);
            if (result.CanSeek)
            {
                result.Seek(0, SeekOrigin.Begin);
            }
            return result;
        }

        /// <summary>Gets the data as a sequence of bytes.</summary>
        /// <returns>A byte array containing the data.</returns>
        public async Task<byte[]> GetBytesAsync()
        {
            using var stream = await GetContentStreamAsync().ConfigureAwait(false);
            return stream switch
            {
                StringReaderStream stringReader => stringReader.Encoding.GetBytes(stringReader.Value),
                _ => new BinaryReader(stream).ReadBytes((int)stream.Length),
            };
        }

        /// <summary>Reads the resource stream as a string.</summary>
        /// <param name="encoding">The character encoding to use.</param>
        /// <returns>The string content of the stream.</returns>
        public async Task<string> ReadAsStringAsync(Encoding? encoding = null)
        {
            using var reader = new StreamReader(await GetContentStreamAsync().ConfigureAwait(false), encoding ?? Encoding.UTF8);
            return reader.ReadToEnd();
        }
    }
}
