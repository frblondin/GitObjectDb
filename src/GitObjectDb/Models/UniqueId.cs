using GitObjectDb.Attributes;
using GitObjectDb.Serialization.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Represents a unique identifier.
    /// </summary>
    [JsonConverter(typeof(UniqueIdConverter))]
    public struct UniqueId : IComparable<UniqueId>, IEquatable<UniqueId>, IEquatable<string>
    {
        /// <summary>
        /// The sha length used by <see cref="UniqueId"/>.
        /// </summary>
        public const int ShaLength = 12;

#pragma warning disable IDE1006 // Naming Styles
        /// <summary>
        /// A read-only instance of the <see cref="UniqueId" /> structure whose value is empty.
        /// </summary>
        public static readonly UniqueId Empty = default;
#pragma warning restore IDE1006 // Naming Styles

        private readonly string _sha;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniqueId"/> struct.
        /// </summary>
        /// <param name="sha">The value.</param>
        public UniqueId(string sha)
        {
            _sha = sha ?? throw new ArgumentNullException(nameof(sha));

            if (!IsShaValid(sha))
            {
                throw new ArgumentException($"The sha should not be null, its length should be exactly {ShaLength}, and it should only contain letters and digits.");
            }
        }

        /// <summary>
        /// Indicates whether the values of two specified <see cref="UniqueId" /> objects are equal.
        /// </summary>
        /// <param name="left">The first object to compare.</param>
        /// <param name="right">The second object to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
        public static bool operator ==(UniqueId left, UniqueId right) => left.Equals(right);

        /// <summary>
        /// Indicates whether the values of two specified <see cref="UniqueId" /> objects are not equal.
        /// </summary>
        /// <param name="left">The first object to compare. </param>
        /// <param name="right">The second object to compare. </param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
        public static bool operator !=(UniqueId left, UniqueId right) => !(left == right);

        /// <summary>Determines whether one specified <see cref="UniqueId" /> is lower than another specified <see cref="UniqueId" />.</summary>
        /// <param name="left">The first object to compare. </param>
        /// <param name="right">The second object to compare. </param>
        /// <returns><see langword="true" /> if <paramref name="left" /> is lower than <paramref name="right" />; otherwise, <see langword="false" />.</returns>
        public static bool operator <(UniqueId left, UniqueId right) => left.CompareTo(right) < 0;

        /// <summary>Determines whether one specified <see cref="UniqueId" /> is the same as or lower than another specified <see cref="UniqueId" />.</summary>
        /// <param name="left">The first object to compare. </param>
        /// <param name="right">The second object to compare. </param>
        /// <returns><see langword="true" /> if <paramref name="left" /> is the same as or lower than <paramref name="right" />; otherwise, <see langword="false" />.</returns>
        public static bool operator <=(UniqueId left, UniqueId right) => left.CompareTo(right) <= 0;

        /// <summary>Determines whether one specified <see cref="UniqueId" /> is greater than another specified <see cref="UniqueId" />.</summary>
        /// <param name="left">The first object to compare. </param>
        /// <param name="right">The second object to compare. </param>
        /// <returns><see langword="true" /> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise, <see langword="false" />.</returns>
        public static bool operator >(UniqueId left, UniqueId right) => left.CompareTo(right) > 0;

        /// <summary>Determines whether one specified <see cref="UniqueId" /> is the same as or greater than another specified <see cref="UniqueId" />.</summary>
        /// <param name="left">The first object to compare. </param>
        /// <param name="right">The second object to compare. </param>
        /// <returns><see langword="true" /> if <paramref name="left" /> is the same as or greater than <paramref name="right" />; otherwise, <see langword="false" />.</returns>
        public static bool operator >=(UniqueId left, UniqueId right) => left.CompareTo(right) >= 0;

        /// <summary>
        /// Creates a new instance of <see cref="UniqueId"/>.
        /// </summary>
        /// <returns>A new <see cref="UniqueId"/>.</returns>
        public static UniqueId CreateNew()
        {
            var chars = CreateNewCharArray();
            return new UniqueId(new string(chars));
        }

        private static char[] CreateNewCharArray()
        {
            var buffer = new byte[ShaLength];
            (new RNGCryptoServiceProvider()).GetBytes(buffer);

            var result = new char[ShaLength];
            for (var pos = 0; pos < ShaLength; pos++)
            {
                result[pos] = ConvertToChar(buffer[pos]);
            }
            return result;
        }

        private static char ConvertToChar(byte value)
        {
            var i = value % 37;
            if (i < 10)
            {
                return (char)('0' + i);
            }
            if (i == 10)
            {
                return '_';
            }
            return (char)('a' + i - 11);
        }

        /// <summary>
        /// Converts the specified string representation to its <see cref="UniqueId" /> equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="s">A string containing a sha to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="UniqueId" /> value equivalent to the sha contained in <paramref name="s" />, if the conversion succeeded, or default if the conversion failed. The conversion fails if the <paramref name="s" /> parameter is <see langword="null" />, is an empty string (""), or does not contain a valid string representation of a sha. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true" /> if the <paramref name="s" /> parameter was converted successfully; otherwise, <see langword="false" />.</returns>
        [ExcludeFromGuardForNull]
        public static bool TryParse(string s, out UniqueId result)
        {
            if (IsShaValid(s))
            {
                result = new UniqueId(s);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        private static bool IsShaValid(string sha) =>
            sha?.Length == ShaLength &&
            sha.All(c => IsShaValidChat(c));

        private static bool IsShaValidChat(char c) =>
            (c >= 'a' && c <= 'a' + 26) ||
            c == '_' ||
            (c >= '0' && c <= '9');

        /// <inheritdoc/>
        public bool Equals(string other) => StringComparer.Ordinal.Equals(_sha, other);

        /// <inheritdoc/>
        public bool Equals(UniqueId other) => StringComparer.Ordinal.Equals(_sha, other._sha);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is UniqueId id && Equals(id);

        /// <inheritdoc/>
        public override int GetHashCode() => _sha != null ? StringComparer.Ordinal.GetHashCode(_sha) : 0;

        /// <inheritdoc/>
        public override string ToString() => _sha ?? string.Empty;

        /// <inheritdoc/>
        public int CompareTo(UniqueId other) => StringComparer.Ordinal.Compare(_sha, other._sha);
    }
}
