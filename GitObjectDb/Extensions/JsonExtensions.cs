using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Newtonsoft.Json
{
    internal static class JsonExtensions
    {
        internal static T ToJson<T>(this Stream stream, JsonSerializer serializer = null)
        {
            if (serializer == null) serializer = new JsonSerializer();
            using (var streamReader = new StreamReader(stream))
            {
                return (T)serializer.Deserialize(streamReader, typeof(T));
            }
        }
    }
}
