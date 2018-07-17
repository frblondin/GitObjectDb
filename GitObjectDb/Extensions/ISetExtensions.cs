using System;
using System.Collections.Generic;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// A set of methods for instances of <see cref="ISet{T}"/>.
    /// </summary>
    internal static class ISetExtensions
    {
        /// <summary>
        /// Adds element to the current set.
        /// </summary>
        /// <typeparam name="T">The type of the set items.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="values">The values.</param>
        public static void AddRange<T>(this ISet<T> source, IEnumerable<T> values)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach (var v in values)
            {
                source.Add(v);
            }
        }
    }
}
