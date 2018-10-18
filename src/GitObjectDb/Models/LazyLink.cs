using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace GitObjectDb.Models
{
    /// <inheritdoc />
    [DebuggerDisplay("IsLinkCreated = {IsLinkCreated}")]
    [DataContract]
    public sealed class LazyLink<TLink> : ILazyLink<TLink>, IEquatable<LazyLink<TLink>>
        where TLink : class, IModelObject
    {
        static readonly string _nullReturnedValueExceptionMessage =
            $"Value returned by {nameof(LazyLink<TLink>)} was null.";

        readonly TLink _link;
        readonly Func<IModelObject, TLink> _factory;
        ObjectPath _path;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyLink{TLink}"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public LazyLink(Func<IModelObject, TLink> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyLink{TLink}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">value</exception>
        public LazyLink(TLink value)
        {
            _link = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyLink{TLink}"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        [JsonConstructor]
        public LazyLink(ObjectPath path)
            : this(parent => (TLink)parent.Repository.GetFromGitPath(path))
        {
            _path = path;
        }

        /// <inheritdoc />
        public IModelObject Parent { get; private set; }

        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TLink Link
        {
            get
            {
                ThrowIfNoParent();

                return _link ?? ResolveLink();
            }
        }

        /// <inheritdoc />
        IModelObject ILazyLink.Link => Link;

        /// <inheritdoc />
        [DataMember]
        public ObjectPath Path => _path ?? new ObjectPath(Link);

        /// <inheritdoc />
        public bool IsLinkCreated => _link != null;

        void ThrowIfNoParent()
        {
            if (Parent == null)
            {
                throw new GitObjectDbException($"Parent is not attached to {nameof(LazyLink<TLink>)}.");
            }
        }

        /// <summary>
        /// Resolves the link. The result should not be stored in a cache (backing field)
        /// since the target object might be stored in another repository that could change
        /// over time (pull, branch...).
        /// </summary>
        /// <returns>Target linked object.</returns>
        TLink ResolveLink()
        {
            var result = GetValueFromFactory(Parent);
            _path = new ObjectPath(result);
            return result;
        }

        TLink GetValueFromFactory(IModelObject parent)
        {
            if (_factory == null)
            {
                throw new GitObjectDbException("Factory cannot be null.");
            }
            return _factory(parent) ?? throw new ObjectNotFoundException(_nullReturnedValueExceptionMessage);
        }

        /// <inheritdoc />
        public ILazyLink<TLink> AttachToParent(IModelObject parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (Parent != null && Parent != parent)
            {
                throw new GitObjectDbException("A single model object cannot be attached to two different parents.");
            }

            Parent = parent;
            return this;
        }

        /// <inheritdoc />
        public object Clone() => _factory != null ?
                                 new LazyLink<TLink>(_factory) :
                                 new LazyLink<TLink>(Path);

        /// <inheritdoc />
        public bool Equals(LazyLink<TLink> other)
        {
            if (other == null)
            {
                return false;
            }

            if (!IsLinkCreated && !other.IsLinkCreated)
            {
                return true; // No better way to spare performance...
            }

            return Path.Equals(Path);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as LazyLink<TLink>);

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        /// <inheritdoc />
        public override int GetHashCode() => throw new NotImplementedException();
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }
}
