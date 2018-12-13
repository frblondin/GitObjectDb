using GitObjectDb.Attributes;
using GitObjectDb.Models;
using Microsoft.Extensions.DependencyInjection;
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
        /// <summary>
        /// Creates a new instance of <see cref="ConstructorParameterBinding"/>.
        /// </summary>
        /// <param name="constructor">The constructor.</param>
        /// <returns>The newly created instance.</returns>
        internal delegate ConstructorParameterBinding Factory(ConstructorInfo constructor);

        private static readonly MethodInfo _serviceProviderGetServiceMethod = ExpressionReflector.GetMethod<IServiceProvider>(s => s.GetService(default));
        private static readonly MethodInfo _childProcessorInvokeMethod = ExpressionReflector.GetMethod<ChildProcessor>(p => p.Invoke(default, default, default, default));

        private static readonly ParameterExpression _sourceObjectArg = Expression.Parameter(typeof(IModelObject), "sourceObject");
        private static readonly ParameterExpression _processArgumentArg = Expression.Parameter(typeof(ProcessArgument), "processArgument");
        private static readonly ParameterExpression _childProcessorArg = Expression.Parameter(typeof(ChildProcessor), "childProcessor");

        private readonly ParameterExpression _typedSourceObjectVar;
        private readonly ParameterExpression _resultVar;

        private readonly IServiceProvider _serviceProvider;
        private readonly IModelDataAccessorProvider _dataAccessorProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorParameterBinding"/> class.
        /// </summary>
        /// <param name="constructor">The constructor.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="dataAccessorProvider">The data accessor provider.</param>
        [ActivatorUtilitiesConstructor]
        public ConstructorParameterBinding(ConstructorInfo constructor, IServiceProvider serviceProvider, IModelDataAccessorProvider dataAccessorProvider)
        {
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dataAccessorProvider = dataAccessorProvider ?? throw new ArgumentNullException(nameof(dataAccessorProvider));
            Parameters = constructor.GetParameters().ToImmutableList();
            _typedSourceObjectVar = Expression.Variable(Constructor.DeclaringType);
            _resultVar = Expression.Variable(Constructor.DeclaringType);

            ClonerExpression = ComputeValueRetrievers();
            Cloner = ClonerExpression.Compile();
        }

        /// <summary>
        /// Processes all children provided in <see cref="ILazyChildren"/>.
        /// </summary>
        /// <param name="childProperty">The child property.</param>
        /// <param name="children">The children.</param>
        /// <param name="new">The new.</param>
        /// <param name="dataAccessor">The data accessor.</param>
        /// <returns>The new <see cref="ILazyChildren"/>.</returns>
        internal delegate ILazyChildren ChildProcessor(ChildPropertyInfo childProperty, ILazyChildren children, IModelObject @new, IModelDataAccessor dataAccessor);

        /// <summary>
        /// Clones an existing <see cref="IModelObject"/> into a new instance after applying the changes contained in a predicate.
        /// </summary>
        /// <param name="object">The object.</param>
        /// <param name="processArgument">The argument processor.</param>
        /// <param name="processor">The processor.</param>
        /// <returns>The newly created instance.</returns>
        internal delegate IModelObject Clone(IModelObject @object, ProcessArgument processArgument, ChildProcessor processor);

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

        private Expression<Clone> ComputeValueRetrievers()
        {
            var properties = Constructor.DeclaringType.GetTypeInfo().GetProperties();
            var dataProvider = _dataAccessorProvider.Get(Constructor.DeclaringType);
            return Expression.Lambda<Clone>(
#pragma warning disable S3220 // Method calls should not resolve ambiguously to overloads with "params"
                Expression.Block(
                    new[] { _typedSourceObjectVar, _resultVar },
                    Expression.Assign(_typedSourceObjectVar, Expression.Convert(_sourceObjectArg, _typedSourceObjectVar.Type)),
                    Expression.Assign(
                        _resultVar,
                        Expression.New(
                            Constructor,
                            from p in Parameters select ResolveArgument(p, properties, dataProvider)))),
#pragma warning restore S3220 // Method calls should not resolve ambiguously to overloads with "params"
                _sourceObjectArg, _processArgumentArg, _childProcessorArg);
        }

        private Expression ResolveArgument(ParameterInfo parameter, PropertyInfo[] properties, IModelDataAccessor dataProvider)
        {
            if (LazyChildrenHelper.TryGetLazyChildrenInterface(parameter.ParameterType) != null)
            {
                return ResolveArgumentForLazyChildren(parameter, dataProvider);
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

        private Expression ResolveArgumentForLazyChildren(ParameterInfo parameter, IModelDataAccessor dataProvider)
        {
            // childProcessor(lazyChildren, result, childDataProvider)
            var property = dataProvider.ChildProperties.TryGetWithValue(p => p.Name, parameter.Name) ??
                throw new GitObjectDbException($"A property _{parameter.Name} was expected to be existing for argument {parameter.Name}.");

            var childType = parameter.ParameterType.GetGenericArguments()[0];
            var childDataProvider = _dataAccessorProvider.Get(childType);

            return Expression.Convert(
                Expression.Call(
                    _childProcessorArg,
                    _childProcessorInvokeMethod,
                    Expression.Constant(property),
                    Expression.Property(_typedSourceObjectVar, property.Property),
                    _resultVar,
                    Expression.Constant(childDataProvider)),
                property.Property.PropertyType);
        }

        private Expression ResolveArgumentFromServiceProvider(ParameterInfo parameter)
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
            return Expression.Convert(
                Expression.Call(
                    _processArgumentArg,
                    _processArgumentArg.Type.GetMethod("Invoke"),
                    _typedSourceObjectVar,
                    Expression.Constant(property.Name),
                    Expression.Constant(parameter.ParameterType),
                    Expression.Convert(Expression.Property(_typedSourceObjectVar, property), typeof(object))),
                parameter.ParameterType);
        }
    }
}
