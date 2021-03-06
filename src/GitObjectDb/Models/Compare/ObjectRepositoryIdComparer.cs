using GitObjectDb.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Models.Compare
{
    /// <summary>
    /// Compares model objects only by using the <see cref="IModelObject.Id"/> property.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <seealso cref="IEqualityComparer{TModel}" />
    public class ObjectRepositoryIdComparer<TModel> : IEqualityComparer<TModel>, IComparer<TModel>
        where TModel : class, IModelObject
    {
        private ObjectRepositoryIdComparer()
        {
        }

#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static ObjectRepositoryIdComparer<TModel> Instance { get; } = new ObjectRepositoryIdComparer<TModel>();
#pragma warning restore CA1000 // Do not declare static members on generic types

        /// <inheritdoc/>
        public int Compare(TModel x, TModel y)
        {
            if (x == null)
            {
                throw new ArgumentNullException(nameof(x));
            }
            if (y == null)
            {
                throw new ArgumentNullException(nameof(y));
            }

            if (x == y)
            {
                return 0;
            }
            return x.Id.CompareTo(y.Id);
        }

        /// <inheritdoc/>
        [ExcludeFromGuardForNull]
        public bool Equals(TModel x, TModel y)
        {
            return x == y ||
                (x != null && y != null && x.Id == y.Id);
        }

        /// <inheritdoc/>
        [ExcludeFromGuardForNull]
        public int GetHashCode(TModel obj)
        {
            return obj?.Id.GetHashCode() ?? 0;
        }
    }
}
