using LibGit2Sharp;
using GitObjectDb.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Models
{
    public interface ILazyChildren
    {
        IEnumerable this[IMetadataObject parent] { get; }
        bool AreChildrenLoaded { get; }
        bool ForceVisit { get; }
    }
    public interface ILazyChildren<TChild> : ILazyChildren
        where TChild : class, IMetadataObject
    {
        new IImmutableList<TChild> this[IMetadataObject parent] { get; }
    }

    public static class LazyChildren
    {
        public static ILazyChildren Create(ChildPropertyInfo propertyInfo, Func<IMetadataObject, Repository, IEnumerable<IMetadataObject>> factory)
        {
            var targetType = typeof(LazyChildren<>).MakeGenericType(propertyInfo.ItemType);
            return (ILazyChildren)Activator.CreateInstance(targetType, factory);
        }
    }

    public class LazyChildren<TChild> : ILazyChildren<TChild>
        where TChild : class, IMetadataObject
    {
        static string NullReturnedValueExceptionMessage =>
            $"Value returned by {nameof(LazyChildren<TChild>)} was null.";

        public bool AreChildrenLoaded => _children != null;
        public bool ForceVisit { get; private set; }

        readonly Func<IMetadataObject, Repository, IEnumerable<IMetadataObject>> _factoryWithRepo;
        readonly Func<IMetadataObject, IEnumerable<TChild>> _factory;

        IImmutableList<TChild> _children;
        public IImmutableList<TChild> this[IMetadataObject parent]
        {
            get
            {
                try
                {
                    if (_children != null) return _children;
                    lock ((object)_factoryWithRepo ?? _factory)
                    {
                        if (_children != null) return _children;
                        _children = _factory == null ?
                            GetValueFromRepositoryFactory(parent) :
                            _factory(parent).ToImmutableList();
                        return _children;
                    }
                }
                finally
                {
                    AttachChildrenToParentIfNeeded(parent);
                }
            }
        }

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

        IEnumerable ILazyChildren.this[IMetadataObject parent] => this[parent];

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

        public LazyChildren<TChild> Clone(TChild except = default, TChild @new = default, Func<TChild, TChild> update = default)
        {
            return new LazyChildren<TChild>(parent =>
                this[parent].Select(c => c == except ?
                    @new :
                    (update?.Invoke(c) ?? c))
                .ToImmutableList())
            {
                ForceVisit = @new != null
            };
        }
    }
}
