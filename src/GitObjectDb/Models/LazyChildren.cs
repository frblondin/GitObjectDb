using GitObjectDb.Attributes;
using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace GitObjectDb.Models
{
    /// <inheritdoc />
    [DebuggerDisplay("AreChildrenLoaded = {AreChildrenLoaded}")]
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public sealed class LazyChildren<TChild> : ILazyChildren<TChild>
#pragma warning restore CA1710 // Identifiers should have correct suffix
        where TChild : class, IModelObject
    {
        static readonly string _nullReturnedValueExceptionMessage =
            $"Value returned by {nameof(LazyChildren<TChild>)} was null.";

        readonly Func<IModelObject, IRepository, IEnumerable<IModelObject>> _factoryWithRepo;
        readonly Func<IModelObject, IEnumerable<TChild>> _factory;
        IImmutableList<TChild> _children;
        bool _parentAttachedInChildren;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <exception cref="ArgumentNullException">factory</exception>
        public LazyChildren(Func<IModelObject, IRepository, IEnumerable<IModelObject>> factory)
        {
            _factoryWithRepo = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <exception cref="ArgumentNullException">factory</exception>
        public LazyChildren(Func<IModelObject, IImmutableList<TChild>> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">value</exception>
        public LazyChildren(IImmutableList<TChild> value)
        {
            _children = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">value</exception>
        public LazyChildren()
            : this(ImmutableList.Create<TChild>())
        {
        }

        /// <inheritdoc />
        public IModelObject Parent { get; private set; }

        /// <inheritdoc />
        public bool AreChildrenLoaded => _children != null;

        /// <inheritdoc />
        public bool ForceVisit { get; private set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IImmutableList<TChild> Children
        {
            get
            {
                ThrowIfNoParent();

                try
                {
                    if (_children != null)
                    {
                        return _children;
                    }

                    var initialized = false;
                    var syncLock = (object)_factoryWithRepo ?? _factory;
                    return LazyInitializer.EnsureInitialized(ref _children, ref initialized, ref syncLock,
                        () => GetValueFromFactory(Parent).ToImmutableList());
                }
                finally
                {
                    AttachChildrenToParentIfNeeded(Parent);
                }
            }
        }

        /// <inheritdoc />
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count => Children.Count;

        /// <inheritdoc />
        public TChild this[int index] => Children[index];

        void ThrowIfNoParent()
        {
            if (Parent == null)
            {
                throw new GitObjectDbException($"Parent is not attached to {nameof(LazyChildren<TChild>)}.");
            }
        }

        IEnumerable<TChild> GetValueFromFactory(IModelObject parent)
        {
            if (_factory != null)
            {
                return _factory(parent) ?? throw new GitObjectDbException(_nullReturnedValueExceptionMessage);
            }
            else if (_factoryWithRepo != null)
            {
                var objectRepository = parent.Repository;
                return objectRepository.RepositoryProvider.Execute(
                    objectRepository.RepositoryDescription,
                    repository =>
                    {
                        var nodes = _factoryWithRepo(parent, repository) ?? throw new GitObjectDbException(_nullReturnedValueExceptionMessage);
                        return nodes.Cast<TChild>();
                    });
            }
            throw new GitObjectDbException("Factory cannot be null.");
        }

        void AttachChildrenToParentIfNeeded(IModelObject parent)
        {
            if (_parentAttachedInChildren || _children == null)
            {
                return;
            }

            foreach (var child in _children)
            {
                child.AttachToParent(parent);
            }
            _parentAttachedInChildren = true;
        }

        /// <inheritdoc />
        [ExcludeFromGuardForNull]
        public ILazyChildren Clone(bool forceVisit, Func<IModelObject, IModelObject> update, IEnumerable<IModelObject> added = null, IEnumerable<IModelObject> deleted = null)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            return new LazyChildren<TChild>(parent =>
                Children
                .Except(deleted?.Cast<TChild>() ?? Enumerable.Empty<TChild>())
                .Select(c => (TChild)update.Invoke(c) ?? throw new ObjectNotFoundException("No child returned while cloning children."))
                .Union(added?.Cast<TChild>() ?? Enumerable.Empty<TChild>())
                .ToImmutableList())
            {
                ForceVisit = ForceVisit || forceVisit
            };
        }

        /// <inheritdoc />
        public ILazyChildren<TChild> AttachToParent(IModelObject parent)
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
            AttachChildrenToParentIfNeeded(parent);
            return this;
        }

        /// <inheritdoc />
        public IEnumerator<TChild> GetEnumerator() => Children.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
