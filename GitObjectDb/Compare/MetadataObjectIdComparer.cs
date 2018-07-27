using GitObjectDb.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// Compares metadata objects only by using the <see cref="IMetadataObject.Id"/> property.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{TModel}" />
    public class MetadataObjectIdComparer<TModel> : IEqualityComparer<TModel>
        where TModel : class, IMetadataObject
    {
        private MetadataObjectIdComparer()
        {
        }

#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static MetadataObjectIdComparer<TModel> Instance { get; } = new MetadataObjectIdComparer<TModel>();
#pragma warning restore CA1000 // Do not declare static members on generic types

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
