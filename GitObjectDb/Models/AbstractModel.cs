using GitObjectDb.Attributes;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Abstract type that any <see cref="IMetadataObject"/> implementation should derive from.
    /// </summary>
    /// <seealso cref="IMetadataObject" />
    [DebuggerDisplay(DebuggerDisplay)]
    [DataContract]
    public abstract class AbstractModel : IMetadataObject
    {
        /// <summary>
        /// The debugger display used by models.
        /// </summary>
        internal const string DebuggerDisplay = "Name = {Name}, Id = {Id}";

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException">
        /// serviceProvider
        /// or
        /// id
        /// or
        /// name
        /// </exception>
        protected AbstractModel(IServiceProvider serviceProvider, Guid id, string name)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var dataAccessorProvider = serviceProvider.GetRequiredService<IModelDataAccessorProvider>();
            DataAccessor = dataAccessorProvider.Get(GetType());
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <inheritdoc />
        public IModelDataAccessor DataAccessor { get; }

        /// <inheritdoc />
        [DataMember]
        public Guid Id { get; }

        /// <inheritdoc />
        [DataMember]
        [Modifiable]
        public string Name { get; }

        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IEnumerable<IMetadataObject> Children => DataAccessor.ChildProperties.SelectMany(p => p.Accessor(this));

        /// <inheritdoc />
        public IMetadataObject Parent { get; private set; }

        /// <summary>
        /// Gets the parent instance.
        /// </summary>
        /// <exception cref="NotSupportedException">No parent repository has been set.</exception>
        public AbstractObjectRepository Repository =>
            this.Root() as AbstractObjectRepository ??
            throw new NotSupportedException("No parent repository has been set.");

        /// <inheritdoc />
        IObjectRepository IMetadataObject.Repository => Repository;

        /// <inheritdoc />
        public void AttachToParent(IMetadataObject parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }
            if (Parent != null && Parent != parent)
            {
                throw new NotSupportedException("A single metadata object cannot be attached to two different parents.");
            }

            Parent = parent;
        }
    }
}
