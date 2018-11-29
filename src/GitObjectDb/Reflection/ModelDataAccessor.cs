using GitObjectDb.Attributes;
using GitObjectDb.JsonConverters;
using GitObjectDb.Models;
using GitObjectDb.Validations;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
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
    /// <inheritdoc />
    internal class ModelDataAccessor : IModelDataAccessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<IImmutableList<ChildPropertyInfo>> _childProperties;
        private readonly Lazy<IImmutableList<ModifiablePropertyInfo>> _modifiableProperties;
        private readonly Lazy<ConstructorParameterBinding> _constructorBinding;
        private readonly ModelObjectSpecialValueProvider _specialValueProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelDataAccessor"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="type">The type.</param>
        /// <exception cref="ArgumentNullException">
        /// serviceProvider
        /// or
        /// IModelDataAccessorProvider
        /// or
        /// type
        /// </exception>
        public ModelDataAccessor(IServiceProvider serviceProvider, Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _childProperties = new Lazy<IImmutableList<ChildPropertyInfo>>(GetChildProperties);
            _modifiableProperties = new Lazy<IImmutableList<ModifiablePropertyInfo>>(GetModifiableProperties);
            _constructorBinding = new Lazy<ConstructorParameterBinding>(() =>
            {
                var constructors = from c in Type.GetTypeInfo().GetConstructors()
                                   select new ConstructorParameterBinding(serviceProvider, c);
                return _serviceProvider.GetRequiredService<IConstructorSelector>().SelectConstructorBinding(Type, constructors.ToArray());
            });
            _specialValueProvider = _serviceProvider.GetRequiredService<ModelObjectSpecialValueProvider>();

            ValidateSerializable();
        }

        /// <inheritdoc />
        public Type Type { get; }

        /// <inheritdoc />
        public IImmutableList<ChildPropertyInfo> ChildProperties => _childProperties.Value;

        /// <inheritdoc />
        public IImmutableList<ModifiablePropertyInfo> ModifiableProperties => _modifiableProperties.Value;

        /// <inheritdoc />
        public ConstructorParameterBinding ConstructorParameterBinding => _constructorBinding.Value;

        private void ValidateSerializable()
        {
            var contract = (JsonObjectContract)JsonSerializerProvider.Default.ContractResolver.ResolveContract(Type);
            var missingMatchingProperties = contract.CreatorParameters.SelectMany(GetParameterErrors).ToList();
            if (missingMatchingProperties.Any())
            {
                throw new NotSupportedException(
                    $"The type {Type.Name} contains invalid constructor parameters:\n\t" +
                    string.Join("\n\t", missingMatchingProperties));
            }

            IEnumerable<string> GetParameterErrors(JsonProperty constructorParameter)
            {
                if (_specialValueProvider.TryGetInjector(Type, constructorParameter) != null)
                {
                    yield break;
                }
                var matching = contract.Properties.TryGetWithValue(p => p.PropertyName, constructorParameter.PropertyName);
                if (matching == null)
                {
                    yield return $"No property named '{constructorParameter.PropertyName}' could be found.";
                }
                if (matching.Ignored)
                {
                    yield return $"The property named '{constructorParameter.PropertyName}' is not serialized.";
                }
                if (matching.PropertyType != constructorParameter.PropertyType)
                {
                    yield return $"The property type '{matching.PropertyType}' does not match.";
                }
            }
        }

        private IImmutableList<ChildPropertyInfo> GetChildProperties() =>
            (from p in Type.GetTypeInfo().GetProperties()
             let lazyChildrenType = LazyChildrenHelper.TryGetLazyChildrenInterface(p.PropertyType)
             where lazyChildrenType != null
             select new ChildPropertyInfo(p, lazyChildrenType.GetGenericArguments()[0]))
            .ToImmutableList();

        private IImmutableList<ModifiablePropertyInfo> GetModifiableProperties() =>
            (from p in Type.GetTypeInfo().GetProperties()
             where Attribute.IsDefined(p, typeof(ModifiableAttribute))
             select new ModifiablePropertyInfo(p))
            .ToImmutableList();

        /// <inheritdoc />
        public IModelObject With(IModelObject source, IPredicateReflector predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var newInstance = DeepClone(
                source.Repository,
                predicate.ProcessArgument,
                predicate.GetChildChanges,
                predicate.MustForceVisit);
            return newInstance.TryGetFromGitPath(source.GetDataPath());
        }

        /// <inheritdoc />
        public IObjectRepository DeepClone(IObjectRepository instance, ProcessArgument processArgument, ChildChangesGetter childChangesGetter, Func<IModelObject, bool> mustForceVisit)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return (IObjectRepository)DeepClone((IModelObject)instance, processArgument, childChangesGetter, mustForceVisit);
        }

        private IModelObject DeepClone(IModelObject node, ProcessArgument processArgument, ChildChangesGetter childChangesGetter, Func<IModelObject, bool> mustForceVisit)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }
            if (processArgument == null)
            {
                throw new ArgumentNullException(nameof(processArgument));
            }
            if (childChangesGetter == null)
            {
                throw new ArgumentNullException(nameof(childChangesGetter));
            }
            if (mustForceVisit == null)
            {
                throw new ArgumentNullException(nameof(mustForceVisit));
            }

            ILazyChildren ProcessChildren(ChildPropertyInfo childProperty, ILazyChildren children, IModelObject @new, IModelDataAccessor childDataAccessor)
            {
                var childChanges = childChangesGetter.Invoke(node, childProperty);
                return children.Clone(
                    forceVisit: mustForceVisit(children.Parent),
                    update: n => DeepClone(n, processArgument, childChangesGetter, mustForceVisit),
                    added: childChanges.Additions,
                    deleted: childChanges.Deletions);
            }

            return node.DataAccessor.ConstructorParameterBinding.Cloner(node, processArgument, ProcessChildren);
        }
    }
}
