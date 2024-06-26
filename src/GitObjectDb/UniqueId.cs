using GitObjectDb.Tools;
using System;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace GitObjectDb;

/// <summary>
/// Represents a unique identifier.
/// </summary>
public struct UniqueId : IComparable<UniqueId>, IEquatable<UniqueId>
{
#if NET48_OR_GREATER
    private static readonly RNGCryptoServiceProvider _rngCryptoServiceProvider = new();
#endif

#pragma warning disable IDE1006 // Naming Styles

    /// <summary>
    /// A read-only instance of the <see cref="UniqueId" /> structure whose value is empty.
    /// </summary>
    public static readonly UniqueId Empty = default;
#pragma warning restore IDE1006

    private readonly string _sha;

    /// <summary>
    /// Initializes a new instance of the <see cref="UniqueId"/> struct.
    /// </summary>
    /// <param name="sha">The value.</param>
    public UniqueId(string sha)
    {
        _sha = sha;

        if (!IsShaValid(sha))
        {
            throw new ArgumentException($"The sha should not be null and it should only contain letters and digits.");
        }
    }

    /// <summary>Gets the sha length used by <see cref="UniqueId"/>.</summary>
    public static int ShaDefaultLength { get; } = 12;

    internal static ConstructorInfo Constructor { get; } =
        ExpressionReflector.GetConstructor(() => new UniqueId(string.Empty));

    /// <summary>
    /// Indicates whether the values of two specified <see cref="UniqueId" /> objects are equal.
    /// </summary>
    /// <param name="left">The first object to compare.</param>
    /// <param name="right">The second object to compare.</param>
    /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(UniqueId left, UniqueId right) =>
        left.Equals(right);

    /// <summary>
    /// Indicates whether the values of two specified <see cref="UniqueId" /> objects are not equal.
    /// </summary>
    /// <param name="left">The first object to compare. </param>
    /// <param name="right">The second object to compare. </param>
    /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(UniqueId left, UniqueId right) =>
        !(left == right);

    /// <summary>Determines whether one specified <see cref="UniqueId" /> is lower than another specified <see cref="UniqueId" />.</summary>
    /// <param name="left">The first object to compare. </param>
    /// <param name="right">The second object to compare. </param>
    /// <returns><see langword="true" /> if <paramref name="left" /> is lower than <paramref name="right" />; otherwise, <see langword="false" />.</returns>
    public static bool operator <(UniqueId left, UniqueId right) =>
        left.CompareTo(right) < 0;

    /// <summary>Determines whether one specified <see cref="UniqueId" /> is the same as or lower than another specified <see cref="UniqueId" />.</summary>
    /// <param name="left">The first object to compare. </param>
    /// <param name="right">The second object to compare. </param>
    /// <returns><see langword="true" /> if <paramref name="left" /> is the same as or lower than <paramref name="right" />; otherwise, <see langword="false" />.</returns>
    public static bool operator <=(UniqueId left, UniqueId right) =>
        left.CompareTo(right) <= 0;

    /// <summary>Determines whether one specified <see cref="UniqueId" /> is greater than another specified <see cref="UniqueId" />.</summary>
    /// <param name="left">The first object to compare. </param>
    /// <param name="right">The second object to compare. </param>
    /// <returns><see langword="true" /> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise, <see langword="false" />.</returns>
    public static bool operator >(UniqueId left, UniqueId right) =>
        left.CompareTo(right) > 0;

    /// <summary>Determines whether one specified <see cref="UniqueId" /> is the same as or greater than another specified <see cref="UniqueId" />.</summary>
    /// <param name="left">The first object to compare. </param>
    /// <param name="right">The second object to compare. </param>
    /// <returns><see langword="true" /> if <paramref name="left" /> is the same as or greater than <paramref name="right" />; otherwise, <see langword="false" />.</returns>
    public static bool operator >=(UniqueId left, UniqueId right) =>
        left.CompareTo(right) >= 0;

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
#if NET48_OR_GREATER
        var buffer = new byte[ShaDefaultLength];
        _rngCryptoServiceProvider.GetBytes(buffer);
#else
        var buffer = RandomNumberGenerator.GetBytes(ShaDefaultLength);
#endif

        var result = new char[ShaDefaultLength];
        for (var pos = 0; pos < ShaDefaultLength; pos++)
        {
            result[pos] = ConvertToChar(buffer[pos]);
        }
        return result;
    }

    private static char ConvertToChar(byte value)
    {
        var i = value % 36;
        if (i < 10)
        {
            return (char)('0' + i);
        }
        return (char)('a' + i - 10);
    }

    /// <summary>
    /// Converts the specified string representation to its <see cref="UniqueId" /> equivalent and returns a value that indicates whether the conversion succeeded.
    /// </summary>
    /// <param name="s">A string containing a sha to convert.</param>
    /// <param name="result">When this method returns, contains the <see cref="UniqueId" /> value equivalent to the sha contained in <paramref name="s" />, if the conversion succeeded, or default if the conversion failed. The conversion fails if the <paramref name="s" /> parameter is <see langword="null" />, is an empty string (""), or does not contain a valid string representation of a sha. This parameter is passed uninitialized.</param>
    /// <returns><see langword="true" /> if the <paramref name="s" /> parameter was converted successfully; otherwise, <see langword="false" />.</returns>
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
        !string.IsNullOrWhiteSpace(sha) &&
        sha.All(IsShaValidChat);

    private static bool IsShaValidChat(char c) =>
        (c >= 'a' && c <= 'z') ||
        (c >= 'A' && c <= 'Z') ||
        (c >= '0' && c <= '9') ||
        c == '_' || c == '-';

    /// <inheritdoc/>
    public bool Equals(UniqueId other) => StringComparer.Ordinal.Equals(_sha, other._sha);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is UniqueId id && Equals(id);

    /// <inheritdoc/>
    public override int GetHashCode() => _sha != null ? StringComparer.Ordinal.GetHashCode(_sha) : 0;

    /// <inheritdoc/>
    public override string ToString() => _sha ?? string.Empty;

    /// <inheritdoc/>
    public int CompareTo(UniqueId other) => string.CompareOrdinal(_sha, other._sha);
}
