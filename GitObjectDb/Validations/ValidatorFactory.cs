using FluentValidation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Validations
{
    /// <inheritdoc />
    public class ValidatorFactory : IValidatorFactory
    {
        readonly IServiceProvider _serviceProvider;
        readonly IList<(Type ValidatorType, Type TargetType)> _validators;
        readonly ConcurrentDictionary<Type, IValidator> _cache = new ConcurrentDictionary<Type, IValidator>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatorFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ValidatorFactory(IServiceProvider serviceProvider)
            : this(serviceProvider, Assembly.GetExecutingAssembly())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatorFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="assemblies">The assemblies.</param>
        public ValidatorFactory(IServiceProvider serviceProvider, params Assembly[] assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            _validators = (from a in assemblies
                           where !a.IsDynamic
                           from t in a.GetExportedTypes()
                           where !t.IsAbstract
                           let genericInterface = t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                           where genericInterface != null
                           select (t, genericInterface.GetGenericArguments()[0])).ToList();
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        static (int? Distance, Type ConcreteType) ComputeScore(Type type, Type validatorType, Type validatorTargetType)
        {
            if (validatorTargetType == type)
            {
                // Best score
                return (0, validatorType);
            }
            else if (validatorType.IsGenericTypeDefinition)
            {
                var genericParameters = validatorType.GetGenericArguments();
                TryAdaptGenericParameters(validatorTargetType, type, genericParameters);
                if (genericParameters.All(p => !p.IsGenericParameter))
                {
                    var adapted = validatorType.MakeGenericType(genericParameters);

                    // Slightly lower score than a perfect match
                    return (adapted != null ? (int?)1 : null, adapted);
                }
                else
                {
                    return (null, null);
                }
            }
            else
            {
                // Slightly lower score than a perfect match or a generic type match
                return (validatorTargetType.IsAssignableFrom(type) ? (int?)2 : null, validatorType);
            }
        }

        static void TryAdaptGenericParameters(Type validatorTypeChunk, Type typeChunk, IList<Type> genericParameters)
        {
            if (validatorTypeChunk.IsGenericParameter)
            {
                TryAdaptGenericParameter(validatorTypeChunk, typeChunk, genericParameters);
            }
            else if (validatorTypeChunk.IsGenericType)
            {
                TryAdaptGenericParameterArgs(validatorTypeChunk, typeChunk, genericParameters);
            }
        }

        static void TryAdaptGenericParameter(Type validatorTypeChunk, Type typeChunk, IList<Type> genericParameters)
        {
            var validConstraints = validatorTypeChunk.GetGenericParameterConstraints().All(constraint => constraint.IsAssignableFrom(typeChunk));
            if (validConstraints)
            {
                var index = genericParameters.IndexOf(validatorTypeChunk);
                if (index != -1)
                {
                    genericParameters[index] = typeChunk;
                }
            }
        }

        static void TryAdaptGenericParameterArgs(Type validatorTypeChunk, Type typeChunk, IList<Type> genericParameters)
        {
            var definition = validatorTypeChunk.GetGenericTypeDefinition();
            var matchingTypeDefinition = FlattenInterfacesAndBaseTypes(typeChunk).FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == definition);
            if (matchingTypeDefinition != null)
            {
                var validatorGenericArgs = validatorTypeChunk.GetGenericArguments();
                var typeGenericArgs = matchingTypeDefinition.GetGenericArguments();
                for (int i = 0; i < validatorGenericArgs.Length; i++)
                {
                    TryAdaptGenericParameters(validatorGenericArgs[i], typeGenericArgs[i], genericParameters);
                }
            }
        }

        static IEnumerable<Type> FlattenInterfacesAndBaseTypes(Type type)
        {
            yield return type;
            foreach (var i in type.GetInterfaces())
            {
                yield return i;
            }
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }

        /// <inheritdoc />
        public IValidator<T> GetValidator<T>() => (IValidator<T>)GetValidator(typeof(T));

        /// <inheritdoc />
        public IValidator GetValidator(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return _cache.GetOrAdd(type, Resolve);
        }

        /// <summary>
        /// Resolves the <see cref="IValidator"/> associated to the specified target type.
        /// </summary>
        /// <param name="targetType">Type of the target.</param>
        /// <returns>The validator.</returns>
        protected virtual IValidator Resolve(Type targetType)
        {
            var type = (from v in _validators
                        let score = ComputeScore(targetType, v.ValidatorType, v.TargetType)
                        where score.Distance != null
                        orderby score.Distance
                        select score.ConcreteType).FirstOrDefault();

            if (type == null)
            {
                throw new KeyNotFoundException($"Could not find any validator for type {targetType}.");
            }

            return InstantiateValidator(type);
        }

        IValidator InstantiateValidator(Type type)
        {
            var singletonProperty = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            if (singletonProperty != null && singletonProperty.CanRead)
            {
                return (IValidator)singletonProperty.GetValue(null);
            }

            var constructor = type.GetConstructor(new[] { typeof(IServiceProvider) });
            if (constructor != null)
            {
                return (IValidator)constructor.Invoke(new object[] { _serviceProvider });
            }

            constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                return (IValidator)constructor.Invoke(Array.Empty<object>());
            }

            throw new KeyNotFoundException($"Could not find a valid constructor for validator {type}.");
        }
    }
}
