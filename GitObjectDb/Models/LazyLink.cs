using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Models
{
    /// <inheritdoc />
    [DebuggerDisplay("IsLinkCreated = {IsLinkCreated}")]
    [DataContract]
    public sealed class LazyLink<TLink> : ILazyLink<TLink>, IEquatable<LazyLink<TLink>>
        where TLink : class, IMetadataObject
    {
        static readonly string _nullReturnedValueExceptionMessage =
            $"Value returned by {nameof(LazyLink<TLink>)} was null.";

        readonly object _syncLock = new object();
        readonly Func<IMetadataObject, TLink> _factory;
        string _path;
        TLink _link;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyLink{TLink}"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public LazyLink(Func<IMetadataObject, TLink> factory)
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
        public LazyLink(string path)
            : this(parent => (TLink)parent.Repository.GetFromGitPath(path))
        {
            _path = path;
        }

        /// <inheritdoc />
        public IMetadataObject Parent { get; private set; }

        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TLink Link
        {
            get
            {
                if (Parent == null)
                {
                    throw new NotSupportedException($"Parent is not attached to {nameof(LazyLink<TLink>)}.");
                }

                if (_link != null)
                {
                    return _link;
                }

                lock (_syncLock)
                {
                    if (_link != null)
                    {
                        return _link;
                    }

                    _link = GetValueFromFactory(Parent);
                    _path = _link.GetFolderPath();
                    return _link;
                }
            }
        }

        /// <inheritdoc />
        IMetadataObject ILazyLink.Link => Link;

        /// <inheritdoc />
        [DataMember]
        public string Path => _path ?? Link.GetFolderPath();

        /// <inheritdoc />
        public bool IsLinkCreated => _link != null;

        TLink GetValueFromFactory(IMetadataObject parent)
        {
            if (_factory == null)
            {
                throw new NotSupportedException("Factory cannot be null.");
            }
            return _factory(parent) ?? throw new NotSupportedException(_nullReturnedValueExceptionMessage);
        }

        /// <inheritdoc />
        public ILazyLink<TLink> AttachToParent(IMetadataObject parent)
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

            return Path.Equals(Path, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as LazyLink<TLink>);

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
        /// <inheritdoc />
        public override int GetHashCode() => throw new NotImplementedException();
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
    }
}
