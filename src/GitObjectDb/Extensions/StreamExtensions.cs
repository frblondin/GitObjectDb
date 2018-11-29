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
            return (T)stream.ToJson(typeof(T), serializer);
        }

        /// <summary>
        /// Deserializes data contained in the <see cref="Stream"/> into an instance
        /// of type <paramref name="objectType"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="objectType">The <see cref="Type"/> of object being deserialized.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The instance of <paramref name="objectType" /> being deserialized.</returns>
        internal static object ToJson(this Stream stream, Type objectType, JsonSerializer serializer)
        {
            if (serializer == null)
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            using (var streamReader = new StreamReader(stream))
            {
                return serializer.Deserialize(streamReader, objectType);
            }
        }
    }
}
