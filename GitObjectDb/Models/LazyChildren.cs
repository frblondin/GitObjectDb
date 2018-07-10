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

namespace GitObjectDb.Models
{
    /// <inheritdoc />
    [DebuggerDisplay("AreChildrenLoaded = {AreChildrenLoaded}")]
    public sealed class LazyChildren<TChild> : ILazyChildren<TChild>
        where TChild : class, IMetadataObject
    {
        static readonly string _nullReturnedValueExceptionMessage =
            $"Value returned by {nameof(LazyChildren<TChild>)} was null.";

        readonly Func<IMetadataObject, IRepository, IEnumerable<IMetadataObject>> _factoryWithRepo;
        readonly Func<IMetadataObject, IEnumerable<TChild>> _factory;
        IImmutableList<TChild> _children;
        bool _parentAttachedInChildren;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <exception cref="ArgumentNullException">factory</exception>
        public LazyChildren(Func<IMetadataObject, IRepository, IEnumerable<IMetadataObject>> factory)
        {
            _factoryWithRepo = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LazyChildren{TChild}"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <exception cref="ArgumentNullException">factory</exception>
        public LazyChildren(Func<IMetadataObject, IImmutableList<TChild>> factory)
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
        public IMetadataObject Parent { get; private set; }

        /// <inheritdoc />
        public bool AreChildrenLoaded => _children != null;

        /// <inheritdoc />
        public bool ForceVisit { get; private set; }

        object SyncLock => (object)_factoryWithRepo ?? _factory;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IImmutableList<TChild> Children
        {
            get
            {
                if (Parent == null)
                {
                    throw new NotSupportedException($"Parent is not attached to {nameof(LazyChildrenHelper)}.");
                }

                try
                {
                    if (_children != null)
                    {
                        return _children;
                    }

                    lock (SyncLock)
                    {
                        if (_children != null)
                        {
                            return _children;
                        }

                        _children = _factory == null ?
                            GetValueFromRepositoryFactory(Parent) :
                            _factory(Parent).ToImmutableList();
                        return _children;
                    }
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

        IImmutableList<TChild> GetValueFromRepositoryFactory(IMetadataObject parent)
        {
            if (_factoryWithRepo == null)
            {
                throw new NotSupportedException("Factory cannot be null.");
            }

            var instance = (AbstractInstance)parent.Instance;
            return instance._repositoryProvider.Execute(instance._repositoryDescription, repository =>
            {
                var nodes = _factoryWithRepo(parent, repository) ?? throw new NotSupportedException(_nullReturnedValueExceptionMessage);
                return nodes.Cast<TChild>()
                            .OrderBy(v => v.Id)
                            .ToImmutableList();
            });
        }

        void AttachChildrenToParentIfNeeded(IMetadataObject parent)
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
        public ILazyChildren Clone(bool forceVisit, Func<IMetadataObject, IMetadataObject> update, IEnumerable added = null, IEnumerable deleted = null) =>
            Clone(forceVisit, n => (TChild)update(n), added?.Cast<TChild>(), deleted?.Cast<TChild>());

        /// <inheritdoc />
        [ExcludeFromGuardForNull]
        public ILazyChildren<TChild> Clone(bool forceVisit, Func<TChild, TChild> update, IEnumerable<TChild> added = null, IEnumerable<TChild> deleted = null)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            return new LazyChildren<TChild>(parent =>
                Children
                .Except(deleted ?? Enumerable.Empty<TChild>())
                .Select(c => update.Invoke(c) ?? throw new NotSupportedException("No child returned while cloning children."))
                .Union(added ?? Enumerable.Empty<TChild>())
                .ToImmutableList())
            {
                ForceVisit = ForceVisit || forceVisit
            };
        }

        /// <inheritdoc />
        ILazyChildren<TChild> ILazyChildren<TChild>.AttachToParent(IMetadataObject parent)
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
        bool ILazyChildren.Add(IMetadataObject child) =>
            throw new NotSupportedException($"The {nameof(ILazyChildren.Add)} method should never by called. Its purpose is to be used within a With(...) predicate.");

        /// <inheritdoc />
        bool ILazyChildren.Delete(IMetadataObject child) =>
            throw new NotSupportedException($"The {nameof(ILazyChildren.Delete)} method should never by called. Its purpose is to be used within a With(...) predicate.");

        /// <inheritdoc />
        public IEnumerator<TChild> GetEnumerator() => Children.GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
