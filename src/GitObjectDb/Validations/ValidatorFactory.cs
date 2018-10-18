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
    public partial class ValidatorFactory : IValidatorFactory
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
            var types = _validators.Select(v => new ValidatorComparerItem(targetType, v)).OrderBy(v => v, new ValidatorComparer(targetType));
            var winner = types.FirstOrDefault() ??
                throw new KeyNotFoundException($"Could not find any validator for type {targetType}.");

            return InstantiateValidator(winner.AdaptedType);
        }

        IValidator InstantiateValidator(Type type)
        {
            var singletonProperty = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            if (singletonProperty != null && singletonProperty.CanRead)
            {
                return (IValidator)singletonProperty.GetValue(null);
            }

            var constructor = type.GetTypeInfo().GetConstructor(new[] { typeof(IServiceProvider) });
            if (constructor != null)
            {
                return (IValidator)constructor.Invoke(new object[] { _serviceProvider });
            }

            constructor = type.GetTypeInfo().GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                return (IValidator)constructor.Invoke(Array.Empty<object>());
            }

            throw new KeyNotFoundException($"Could not find a valid constructor for validator {type}.");
        }
    }
}
