using GitObjectDb.Serialization.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Stores the content of a string value in a separate file.
    /// </summary>
    [DataContract]
    [JsonConverter(typeof(StringBlobConverter))]
    public sealed class StringBlob : IBlob<string>, IEquatable<StringBlob>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringBlob"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public StringBlob(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc />
        public string Value { get; }

        object IBlob.Value => Value;

        /// <inheritdoc />
        public bool Equals(StringBlob other) =>
            StringComparer.OrdinalIgnoreCase.Equals(other?.Value, Value);

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as StringBlob);

        /// <inheritdoc />
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        /// <inheritdoc />
        public override string ToString() => Value;
    }
}
