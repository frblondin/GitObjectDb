using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Tools;

/// <summary>Provides helpers for <see cref="Type"/>.</summary>
internal static class TypeHelper
{
    /// <summary>Converts a <paramref name="type"/> to its <see cref="string"/> equivalent.</summary>
    /// <param name="type">The <see cref="Type"/> to be converted.</param>
    /// <returns>The <see cref="string"/> representation of <paramref name="type"/>.</returns>
    public static string BindToName(Type type) => type.FullName;
}
