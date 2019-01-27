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
        private static readonly string _nullReturnedValueExceptionMessage =
            $"Value returned by {nameof(LazyLink<TLink>)} was null.";

        private readonly TLink _link;
        private readonly Func<TLink> _factory;
        private readonly IObjectRepositoryContainer _container;
        private ObjectPath _path;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyLink{TLink}"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="factory">The factory.</param>
        public LazyLink(IObjectRepositoryContainer container, Func<TLink> factory)
            : this(container)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyLink{TLink}"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">value</exception>
        public LazyLink(IObjectRepositoryContainer container, TLink value)
            : this(container)
        {
            _link = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyLink{TLink}"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="path">The path.</param>
        [JsonConstructor]
        public LazyLink(IObjectRepositoryContainer container, ObjectPath path)
            : this(container)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
            _factory = () => (TLink)_container.GetFromGitPath(path);
        }

        private LazyLink(IObjectRepositoryContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public TLink Link => _link ?? ResolveLink();

        /// <inheritdoc />
        IModelObject ILazyLink.Link => Link;

        /// <inheritdoc />
        [DataMember]
        public ObjectPath Path => _path ?? new ObjectPath(Link);

        /// <inheritdoc />
        public bool IsLinkCreated => _link != null;

        /// <summary>
        /// Resolves the link. The result should not be stored in a cache (backing field)
        /// since the target object might be stored in another repository that could change
        /// over time (pull, branch...).
        /// </summary>
        /// <returns>Target linked object.</returns>
        private TLink ResolveLink()
        {
            var result = GetValueFromFactory();
            _path = result.Path;
            return result;
        }

        private TLink GetValueFromFactory()
        {
            if (_factory == null)
            {
                throw new GitObjectDbException("Factory cannot be null.");
            }
            return _factory() ?? throw new ObjectNotFoundException(_nullReturnedValueExceptionMessage);
        }

        /// <inheritdoc />
        public object Clone()
        {
            if (_path != null)
            {
                return new LazyLink<TLink>(_container, _path);
            }
            if (_factory != null)
            {
                return new LazyLink<TLink>(_container, _factory);
            }
            return new LazyLink<TLink>(_container, Path);
        }

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
