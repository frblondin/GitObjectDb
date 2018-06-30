using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using GitObjectDb.Reflection;
using System.Diagnostics;

namespace GitObjectDb.Models
{
    [DebuggerDisplay("AreChildrenLoaded = {AreChildrenLoaded}")]
    public class LazyChildren<TChild> : ILazyChildren<TChild>
        where TChild : class, IMetadataObject
    {
        static string NullReturnedValueExceptionMessage =>
            $"Value returned by {nameof(LazyChildren<TChild>)} was null.";

        public IMetadataObject Parent { get; private set; }
        public bool AreChildrenLoaded => _children != null;
        public bool ForceVisit { get; private set; }

        readonly Func<IMetadataObject, Repository, IEnumerable<IMetadataObject>> _factoryWithRepo;
        readonly Func<IMetadataObject, IEnumerable<TChild>> _factory;

        object SyncLock => (object)_factoryWithRepo ?? _factory;

        IImmutableList<TChild> _children;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        IImmutableList<TChild> Children
        {
            get
            {
                if (Parent == null) throw new NotSupportedException($"Parent is not attached to {nameof(LazyChildren)}.");
                try
                {
                    if (_children != null) return _children;

                    lock (SyncLock)
                    {
                        if (_children != null) return _children;
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Count => Children.Count;

        public TChild this[int index] => Children[index];

        IImmutableList<TChild> GetValueFromRepositoryFactory(IMetadataObject parent)
        {
            if (_factoryWithRepo == null) throw new NotSupportedException("Factory cannot be null.");
            var instance = (AbstractInstance)parent.Instance;
            using (var repository = instance.GetRepository())
            {
                return (_factoryWithRepo(parent, repository) ?? throw new NullReferenceException(NullReturnedValueExceptionMessage))
                    .Cast<TChild>()
                    .OrderBy(v => v.Id)
                    .ToImmutableList();
            }
        }

        public LazyChildren(Func<IMetadataObject, Repository, IEnumerable<IMetadataObject>> factory)
        {
            _factoryWithRepo = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public LazyChildren(Func<IMetadataObject, IImmutableList<TChild>> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public LazyChildren(IImmutableList<TChild> value)
        {
            _children = value ?? throw new ArgumentNullException(nameof(value));
        }

        bool _parentAttachedInChildren;
        void AttachChildrenToParentIfNeeded(IMetadataObject parent)
        {
            if (_parentAttachedInChildren || _children == null) return;
            foreach (var child in _children)
            {
                child.AttachToParent(parent);
            }
            _parentAttachedInChildren = true;
        }

        ILazyChildren ILazyChildren.Clone(Func<IMetadataObject, IMetadataObject> update, bool forceVisit) =>
            Clone(n => (TChild)update(n), forceVisit);
        public LazyChildren<TChild> Clone(Func<TChild, TChild> update, bool forceVisit)
        {
            if (update == null) throw new ArgumentNullException(nameof(update));

            return new LazyChildren<TChild>(parent =>
                Children.Select(c => update.Invoke(c) ?? throw new NullReferenceException("No child returned while cloning children."))
                .ToImmutableList())
            {
                ForceVisit = ForceVisit || forceVisit
            };
        }

        ILazyChildren<TChild> ILazyChildren<TChild>.AttachToParent(IMetadataObject parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (Parent != null && Parent != parent) throw new NotSupportedException("A single metadata object cannot be attached to two different parents.");
            Parent = parent;
            return this;
        }

        public IEnumerator<TChild> GetEnumerator() => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal static class LazyChildren
    {
        internal static ILazyChildren Create(ChildPropertyInfo propertyInfo, Func<IMetadataObject, Repository, IEnumerable<IMetadataObject>> factory)
        {
            var targetType = typeof(LazyChildren<>).MakeGenericType(propertyInfo.ItemType);
            return (ILazyChildren)Activator.CreateInstance(targetType, factory);
        }

        internal static Type TryGetLazyChildrenInterface(Type type) =>
            type.GetInterfaces().Prepend(type).FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ILazyChildren<>));
    }
}
