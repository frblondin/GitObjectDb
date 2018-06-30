using GitObjectDb.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Reflection
{
    public interface IModelDataAccessor
    {
        ImmutableList<ChildPropertyInfo> ChildProperties { get; }
        ImmutableList<ModifiablePropertyInfo> ModifiableProperties { get; }
        ConstructorParameterBinding ConstructorParameterBinding { get; }
        IMetadataObject With(IMetadataObject @object, Expression predicate = null);
    }

    internal class ModelDataAccessor : IModelDataAccessor
    {
        public Type Type { get; }
        readonly IServiceProvider _serviceProvider;
        readonly IModelDataAccessorProvider _dataAccessorProvider;

        readonly Lazy<ImmutableList<ChildPropertyInfo>> _childProperties;
        public ImmutableList<ChildPropertyInfo> ChildProperties => _childProperties.Value;

        readonly Lazy<ImmutableList<ModifiablePropertyInfo>> _modifiableProperties;
        public ImmutableList<ModifiablePropertyInfo> ModifiableProperties => _modifiableProperties.Value;

        readonly Lazy<ConstructorParameterBinding> _constructorBinding;
        public ConstructorParameterBinding ConstructorParameterBinding => _constructorBinding.Value;

        internal ModelDataAccessor(IServiceProvider serviceProvider, Type type)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dataAccessorProvider = _serviceProvider.GetService<IModelDataAccessorProvider>() ?? throw new ArgumentNullException(nameof(IModelDataAccessorProvider));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            _childProperties = new Lazy<ImmutableList<ChildPropertyInfo>>(GetChildProperties);
            _modifiableProperties = new Lazy<ImmutableList<ModifiablePropertyInfo>>(GetModifiableProperties);
            _constructorBinding = new Lazy<ConstructorParameterBinding>(() =>
            {
                var constructors = from c in Type.GetConstructors()
                                   select new ConstructorParameterBinding(_serviceProvider, c);
                return serviceProvider.GetService<IConstructorSelector>().SelectConstructorBinding(Type, constructors.ToArray());
            });
        }

        ImmutableList<ChildPropertyInfo> GetChildProperties() =>
            (from p in Type.GetProperties()
             let lazyChildrenType = LazyChildren.TryGetLazyChildrenInterface(p.PropertyType)
             where lazyChildrenType != null
             select new ChildPropertyInfo(p, lazyChildrenType.GetGenericArguments()[0])).ToImmutableList();

        ImmutableList<ModifiablePropertyInfo> GetModifiableProperties() =>
            (from p in Type.GetProperties()
             where Attribute.IsDefined(p, typeof(ModifiableAttribute))
             select new ModifiablePropertyInfo(p)).ToImmutableList();

        IMetadataObject CloneSubTree(IMetadataObject @object, Expression predicate = null)
        {
            if (@object == null) throw new ArgumentNullException(nameof(@object));

            var reflector = new PredicateReflector(predicate);
            return ConstructorParameterBinding.Cloner(
                @object,
                reflector,
                (children, @new, childDataAccessor) =>
                    children.Clone(n => n == @object ?
                                        @new :
                                        childDataAccessor.ConstructorParameterBinding.Cloner(n, PredicateReflector.Empty, RecursiveClone),
                                   forceVisit: false));
        }

        ILazyChildren RecursiveClone(ILazyChildren children, IMetadataObject @new, IModelDataAccessor childDataAccessor) =>
            children.Clone(n => childDataAccessor.ConstructorParameterBinding.Cloner(n, new PredicateReflector(), RecursiveClone),
                           forceVisit: false);

        void CreateNewParentTree(IMetadataObject old, IMetadataObject @new)
        {
            if (old is AbstractInstance) return;

            if (old == null) throw new ArgumentNullException(nameof(old));
            if (@new == null) throw new ArgumentNullException(nameof(@new));
            if (old.Parent == null) throw new NullReferenceException($"Object '{old}' does not have any parent.");

            var parentDataAccessor = _dataAccessorProvider.Get(old.Parent.GetType());
            var newParent = parentDataAccessor.ConstructorParameterBinding.Cloner(
                old.Parent,
                PredicateReflector.Empty,
                (children, _, childDataAccessor) =>
                    children.Clone(n => n == old ?
                                        @new :
                                        childDataAccessor.ConstructorParameterBinding.Cloner(n, new PredicateReflector(), RecursiveClone),
                                   forceVisit: false));
            @new.AttachToParent(newParent);

            CreateNewParentTree(old.Parent, newParent);
        }

        public IMetadataObject With(IMetadataObject @object, Expression predicate = null)
        {
            var result = CloneSubTree(@object, predicate);
            CreateNewParentTree(@object, result);
            return result;
        }
    }
}
