using GitObjectDb.Serialization.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization.Json
{
    [JsonConverter(typeof(NonScalarConverter))]
    internal class NonScalar
    {
        public NonScalar(Node node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        public Type Type => Node.GetType();

        public Node Node { get; set; }
    }
}
