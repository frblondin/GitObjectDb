using GitObjectDb.Attributes;
using GitObjectDb.Utils;
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

        [DataMember]
        public Guid Id { get; }
        [DataMember, Modifiable]
        public string Name { get; }

        public abstract IEnumerable<IMetadataObject> Children { get; }
        public IMetadataObject Parent { get; private set; }

        public AbstractInstance Instance =>
            this.Root() as AbstractInstance ??
            throw new NullReferenceException("No parent instance has been set.");
        IInstance IMetadataObject.Instance => Instance;

        public AbstractModel(Guid id, string name)
        {
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

        internal IMetadataObject With(Expression predicate = null)
        {
            var result = CloneSubTree(predicate);
            CreateNewParent(result);
            return result;
        }

        protected abstract void CreateNewParent(IMetadataObject @new);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract IMetadataObject CloneSubTree(Expression predicate = null);
    }
}
