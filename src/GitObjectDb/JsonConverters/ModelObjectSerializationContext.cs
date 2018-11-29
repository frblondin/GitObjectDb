using GitObjectDb.Models;
using GitObjectDb.Services;
using System;

namespace GitObjectDb.JsonConverters
{
    /// <summary>
    /// Hosts values being required by <see cref="ModelObjectSpecialValueProvider"/>.
    /// </summary>
    internal class ModelObjectSerializationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelObjectSerializationContext"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="childrenResolver">The children resolver.</param>
        /// <exception cref="ArgumentNullException">
        /// childrenResolver
        /// or
        /// container
        /// </exception>
        public ModelObjectSerializationContext(IObjectRepositoryContainer container, ChildrenResolver childrenResolver = null)
        {
            Container = container ?? throw new ArgumentNullException(nameof(container));
            ChildrenResolver = childrenResolver;
        }

        /// <summary>
        /// Gets the children resolver.
        /// </summary>
        internal ChildrenResolver ChildrenResolver { get; }

        /// <summary>
        /// Gets the container.
        /// </summary>
        internal IObjectRepositoryContainer Container { get; }
    }
}