using System;
using System.Collections.Generic;
using System.Text;

namespace System.Linq
{
    /// <summary>
    /// A set of methods for instances of <see cref="object"/>.
    /// </summary>
    internal static class ObjectExtensions
    {
        /// <summary>
        /// Returns the element as an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the element.</typeparam>
        /// <param name="source">The object instance.</param>
        /// <returns>An enumerable containing only the <paramref name="source"/> instance.</returns>
        internal static IEnumerable<TSource> ToEnumerable<TSource>(this TSource source)
        {
            yield return source;
        }
    }
}
