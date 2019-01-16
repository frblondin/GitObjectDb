using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Serialization.Json.Converters
{
    /// <summary>
    /// <see cref="JsonContract"/> provider that manages cache optimizations.
    /// </summary>
    internal class ModelObjectContractCache
    {
        private readonly ModelObjectSpecialValueProvider _modelObjectSpecialValueProvider;

        private readonly ConcurrentDictionary<Type, JsonContract> _cache =
            new ConcurrentDictionary<Type, JsonContract>();

        private readonly ConcurrentDictionary<Type, Func<ModelObjectSerializationContext, ObjectConstructor<object>>> _creatorProviders =
            new ConcurrentDictionary<Type, Func<ModelObjectSerializationContext, ObjectConstructor<object>>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelObjectContractCache" /> class.
        /// </summary>
        /// <param name="modelObjectSpecialValueProvider">The special value provider.</param>
        public ModelObjectContractCache(ModelObjectSpecialValueProvider modelObjectSpecialValueProvider)
        {
            _modelObjectSpecialValueProvider = modelObjectSpecialValueProvider ?? throw new ArgumentNullException(nameof(modelObjectSpecialValueProvider));
        }

        /// <summary>
        /// Gets the contract for a given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="context">The context.</param>
        /// <returns>The contract for a given type.</returns>
        internal JsonContract GetContract(Type type, ModelObjectSerializationContext context)
        {
            if (!_cache.TryGetValue(type, out var result))
            {
                result = CreateContract(type);
                var updated = OverrideCreatorIfNeeded(type, context, result);
                if (!updated)
                {
                    _cache[type] = result;
                }
            }
            return result;
        }

        private static JsonContract CreateContract(Type type)
        {
            var defaultContractResolver = new DefaultContractResolver();
            return defaultContractResolver.ResolveContract(type);
        }

        private bool OverrideCreatorIfNeeded(Type type, ModelObjectSerializationContext context, JsonContract result)
        {
            if (result is JsonObjectContract objectContract)
            {
                var provider = GetFactoryProvider(type, objectContract);
                if (provider != null)
                {
                    objectContract.OverrideCreator = provider.Invoke(context);
                    return true;
                }
            }
            return false;
        }

        private Func<ModelObjectSerializationContext, ObjectConstructor<object>> GetFactoryProvider(Type type, JsonObjectContract objectContract)
        {
            return _creatorProviders.GetOrAdd(type,
                t => TryCreateFactoryProvider(t, objectContract.CreatorParameters)?.Compile());
        }

        private Expression<Func<ModelObjectSerializationContext, ObjectConstructor<object>>> TryCreateFactoryProvider(Type type, JsonPropertyCollection properties)
        {
            var injectors = (from p in properties select _modelObjectSpecialValueProvider.TryGetInjector(type, p)).ToList();
            if (injectors.Any(i => i != null))
            {
                var closureContext = Expression.Parameter(typeof(ModelObjectSerializationContext));
                return Expression.Lambda<Func<ModelObjectSerializationContext, ObjectConstructor<object>>>(
                    CreateFactory(type, properties, injectors, closureContext),
                    closureContext);
            }
            return null;
        }

        private static Expression<ObjectConstructor<object>> CreateFactory(Type type, JsonPropertyCollection properties, List<Func<ModelObjectSerializationContext, object>> injectors, ParameterExpression context)
        {
            var arrayArg = Expression.Parameter(typeof(object[]));
            var constructor = type.GetConstructor(properties.Select(p => p.PropertyType).ToArray());
            var arguments = properties.Select(CreateArgumentExpression);
            return Expression.Lambda<ObjectConstructor<object>>(
                Expression.New(constructor, arguments),
                arrayArg);

            UnaryExpression CreateArgumentExpression(JsonProperty property, int i) =>
                Expression.Convert(
                    injectors[i] != null ?
                        (Expression)Expression.Invoke(Expression.Constant(injectors[i]), context) :
                        Expression.ArrayIndex(arrayArg, Expression.Constant(i)),
                    property.PropertyType);
        }
    }
}
