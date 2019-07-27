using System;
using System.Text;

namespace System.Collections.Generic
{
    /// <summary>
    /// A set of methods for instances of <see cref="IList{T}"/>.
    /// </summary>
    public static class IListExtensions
    {
        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="source">An <see cref="IList{T}"/> to add elements to.</param>
        /// <param name="collection">The collection whose elements should be added to the end of the <see cref="IList{T}"/>.
        /// The collection itself cannot be null, but it can contain elements that are null,
        /// if type <typeparamref name="T"/> is a reference type.</param>
        /// <returns>The modified list.</returns>
        public static IList<T> AddRange<T>(this IList<T> source, IEnumerable<T> collection)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (var item in collection)
            {
                source.Add(item);
            }
            return source;
        }
    }
}
