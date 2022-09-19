using GitObjectDb.Serialization.Json.Converters;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GitObjectDb.Serialization
{
    [JsonConverter(typeof(NonScalarConverter))]
    internal class NonScalar
    {
        public NonScalar(Node node)
        {
            Node = node;
        }

        public Type Type => Node.GetType();

        public Node Node { get; set; }

        [ExcludeFromCodeCoverage]
        public override string ToString() => $"{Type}, {Node}";
    }
}
