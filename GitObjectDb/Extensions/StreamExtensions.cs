using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Newtonsoft.Json
{
    /// <summary>
    /// A set of methods for instances of <see cref="Stream"/>.
    /// </summary>
    internal static class StreamExtensions
    {
        /// <summary>
        /// Deserializes data contained in the <see cref="Stream"/> into an instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize.</typeparam>
        /// <param name="stream">The stream.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The instance of <typeparamref name="T"/> being deserialized.</returns>
        internal static T ToJson<T>(this Stream stream, JsonSerializer serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            using (var streamReader = new StreamReader(stream))
            {
                return (T)serializer.Deserialize(streamReader, typeof(T));
            }
        }
    }
}
