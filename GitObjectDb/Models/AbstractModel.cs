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
    [DebuggerDisplay(DebuggerDisplay)]
    [DataContract]
    public abstract class AbstractModel : IMetadataObject
    {
        internal const string DebuggerDisplay = "Name = {Name}, Id = {Id}";

        protected IModelDataAccessorProvider DataAccessorProvider { get; }
        readonly IModelDataAccessor _dataAccessor;

        [DataMember]
        public Guid Id { get; }
        [DataMember, Modifiable]
        public string Name { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IEnumerable<IMetadataObject> Children => _dataAccessor.ChildProperties.SelectMany(p => p.Accessor(this));

        public IMetadataObject Parent { get; private set; }

        public AbstractInstance Instance =>
            this.Root() as AbstractInstance ??
            throw new NullReferenceException("No parent instance has been set.");
        IInstance IMetadataObject.Instance => Instance;

        public AbstractModel(IServiceProvider serviceProvider, Guid id, string name)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            DataAccessorProvider = serviceProvider.GetService<IModelDataAccessorProvider>();
            _dataAccessor = DataAccessorProvider.Get(GetType());
            if (id == Guid.Empty) throw new ArgumentNullException(nameof(id));
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        void IMetadataObject.AttachToParent(IMetadataObject parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (Parent != null && Parent != parent) throw new NotSupportedException("A single metadata object cannot be attached to two different parents.");
            Parent = parent;
        }

        IMetadataObject IMetadataObject.With(Expression predicate) =>
            _dataAccessor.With(this, predicate);
    }
}
