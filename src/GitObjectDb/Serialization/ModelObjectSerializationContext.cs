using GitObjectDb.Models;
using GitObjectDb.Services;
using System;

namespace GitObjectDb.Serialization
{
    /// <summary>
    /// Hosts values being required by <see cref="IObjectRepositorySerializer"/>.
    /// </summary>
    public class ModelObjectSerializationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelObjectSerializationContext"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="childrenResolver">The children resolver.</param>
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