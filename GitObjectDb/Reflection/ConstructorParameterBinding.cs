using GitObjectDb.Attributes;
using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace GitObjectDb.Reflection
{
    /// <summary>
    /// Binds a constructor to the parameters that will be used when it is invoked.
    /// </summary>
    public class ConstructorParameterBinding
    {
        static readonly MethodInfo _serviceProviderGetServiceMethod = typeof(IServiceProvider).GetMethod("GetService", BindingFlags.Instance | BindingFlags.Public);
        static readonly MethodInfo _childProcessorInvokeMethod = typeof(ChildProcessor).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public);

        static readonly ParameterExpression _sourceObjectArg = Expression.Parameter(typeof(IMetadataObject), "sourceObject");
        static readonly ParameterExpression _predicateReflectorArg = Expression.Parameter(typeof(PredicateReflector), "predicateReflector");
        static readonly ParameterExpression _childProcessorArg = Expression.Parameter(typeof(ChildProcessor), "childProcessor");

        readonly ParameterExpression _typedSourceObjectVar;
        readonly ParameterExpression _resultVar;

        readonly IServiceProvider _serviceProvider;
        readonly IModelDataAccessorProvider _dataAccessorProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorParameterBinding"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="constructor">The constructor.</param>
        /// <exception cref="ArgumentNullException">
        /// serviceProvider
        /// or
        /// constructor
        /// </exception>
        public ConstructorParameterBinding(IServiceProvider serviceProvider, ConstructorInfo constructor)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dataAccessorProvider = serviceProvider.GetService<IModelDataAccessorProvider>();
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            Parameters = constructor.GetParameters().ToImmutableList();
            _typedSourceObjectVar = Expression.Variable(Constructor.DeclaringType);
            _resultVar = Expression.Variable(Constructor.DeclaringType);

            ClonerExpression = ComputeValueRetrievers();
            Cloner = ClonerExpression.Compile();
        }

        /// <summary>
        /// Processes all children provided in <see cref="ILazyChildren"/>.
        /// </summary>
        /// <param name="children">The children.</param>
        /// <param name="new">The new.</param>
        /// <param name="dataAccessor">The data accessor.</param>
        /// <returns>The new <see cref="ILazyChildren"/>.</returns>
        internal delegate ILazyChildren ChildProcessor(ILazyChildren children, IMetadataObject @new, IModelDataAccessor dataAccessor);

        /// <summary>
        /// Clones an existing <see cref="IMetadataObject"/> into a new instance after applying the changes contained in a predicate.
        /// </summary>
        /// <param name="object">The object.</param>
        /// <param name="predicateReflector">The predicate reflector.</param>
        /// <param name="processor">The processor.</param>
        /// <returns>The newly created instance.</returns>
        internal delegate IMetadataObject Clone(IMetadataObject @object, PredicateReflector predicateReflector, ChildProcessor processor);

        /// <summary>
        /// Gets the constructor.
        /// </summary>
        public ConstructorInfo Constructor { get; }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        public IImmutableList<ParameterInfo> Parameters { get; }

        /// <summary>
        /// Gets the cloner expression.
        /// </summary>
        internal Expression<Clone> ClonerExpression { get; }

        /// <summary>
        /// Gets the cloner function.
        /// </summary>
        internal Clone Cloner { get; }

        Expression<Clone> ComputeValueRetrievers()
        {
            var properties = Constructor.DeclaringType.GetProperties();
            return Expression.Lambda<Clone>(
#pragma warning disable S3220 // Method calls should not resolve ambiguously to overloads with "params"
                Expression.Block(
                    new[] { _typedSourceObjectVar, _resultVar },
                    Expression.Assign(_typedSourceObjectVar, Expression.Convert(_sourceObjectArg, _typedSourceObjectVar.Type)),
                    Expression.Assign(
                        _resultVar,
                        Expression.New(
                            Constructor,
                            from p in Parameters select ResolveArgument(p, properties)))),
#pragma warning restore S3220 // Method calls should not resolve ambiguously to overloads with "params"
                _sourceObjectArg, _predicateReflectorArg, _childProcessorArg);
        }

        Expression ResolveArgument(ParameterInfo parameter, PropertyInfo[] properties)
        {
            if (LazyChildrenHelper.TryGetLazyChildrenInterface(parameter.ParameterType) != null)
            {
                return ResolveArgumentForLazyChildren(parameter);
            }
            else
            {
                var property = properties.FirstOrDefault(p =>
                    p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase) &&
                    p.PropertyType == parameter.ParameterType);
                if (property == null)
                {
                    return ResolveArgumentFromServiceProvider(parameter);
                }
                else
                {
                    return ResolveArgumentFromReflector(parameter, property);
                }
            }
        }

        Expression ResolveArgumentForLazyChildren(ParameterInfo parameter)
        {
            // childProcessor(lazyChildren, result, childDataProvider)
            var property = Constructor.DeclaringType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => p.Name.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase)) ??
                throw new NotSupportedException($"A property _{parameter.Name} was expected to be existing for argument {parameter.Name}.");

            var childType = parameter.ParameterType.GetGenericArguments()[0];
            var childDataProvider = _dataAccessorProvider.Get(childType);

            return Expression.Convert(
                Expression.Call(
                    _childProcessorArg,
                    _childProcessorInvokeMethod,
                    Expression.Property(_typedSourceObjectVar, property),
                    _resultVar,
                    Expression.Constant(childDataProvider)),
                property.PropertyType);
        }

        Expression ResolveArgumentFromServiceProvider(ParameterInfo parameter)
        {
            // serviceProvider.GetService(typeof(parameterType))
            return Expression.Convert(
                Expression.Call(
                    Expression.Constant(_serviceProvider),
                    _serviceProviderGetServiceMethod,
                    Expression.Constant(parameter.ParameterType)),
                parameter.ParameterType);
        }

        Expression ResolveArgumentFromReflector(ParameterInfo parameter, PropertyInfo property)
        {
            // predicate.ProcessArgument(propertyName, @object)
            return Expression.Call(
                _predicateReflectorArg,
                PredicateReflector.ProcessArgumentMethod.MakeGenericMethod(parameter.ParameterType),
                Expression.Constant(property.Name),
                Expression.Property(_typedSourceObjectVar, property));
        }
    }
}
