using System;
using System.Collections.Generic;
using System.Text;

namespace System.Linq
{
    /// <summary>
    /// A set of methods for instances of <see cref="IEnumerable{T}"/>.
    /// </summary>
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Returns the first element whose property string value matches the <paramref name="value"/> argument.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">An <see cref="IEnumerable{T}"/> to return an element from.</param>
        /// <param name="propertyAccessor">A function to extract the element value to be compared against <paramref name="value"/>.</param>
        /// <param name="value">The expected value.</param>
        /// <param name="comparisonType">One of the enumeration values that specifies the rules for the comparison.</param>
        /// <returns><code>default(TSource)</code> if source is empty or if no element property value is equal to
        /// <paramref name="value"/>; otherwise, the first element in source that matches.</returns>
        public static TSource TryGetWithValue<TSource>(this IEnumerable<TSource> source, Func<TSource, string> propertyAccessor, string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (propertyAccessor == null)
            {
                throw new ArgumentNullException(nameof(propertyAccessor));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return source.FirstOrDefault(o => string.Equals(propertyAccessor(o), value, comparisonType));
        }
    }
}
