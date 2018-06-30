using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using GitObjectDb.Attributes;
using GitObjectDb.Models;

namespace GitObjectDb.Reflection
{
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

        public ConstructorInfo Constructor { get; }
        public ParameterInfo[] Parameters { get; }

        internal delegate ILazyChildren ChildProcessor(ILazyChildren children, IMetadataObject @new, IModelDataAccessor dataAccessor);
        internal delegate IMetadataObject Clone(IMetadataObject @object, PredicateReflector predicateReflector, ChildProcessor processor);
        internal Expression<Clone> ClonerExpression { get; }
        internal Clone Cloner { get; }

        public ConstructorParameterBinding(IServiceProvider serviceProvider, ConstructorInfo constructor)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _dataAccessorProvider = serviceProvider.GetService<IModelDataAccessorProvider>();
            Constructor = constructor ?? throw new ArgumentNullException(nameof(constructor));
            Parameters = constructor.GetParameters();
            _typedSourceObjectVar = Expression.Variable(Constructor.DeclaringType);
            _resultVar = Expression.Variable(Constructor.DeclaringType);

            ClonerExpression = ComputeValueRetrievers();
            Cloner = ClonerExpression.Compile();
        }

        Expression<Clone> ComputeValueRetrievers()
        {
            var properties = Constructor.DeclaringType.GetProperties();
            return Expression.Lambda<Clone>(
                Expression.Block(
                    new[] { _typedSourceObjectVar, _resultVar },
                    Expression.Assign(_typedSourceObjectVar, Expression.Convert(_sourceObjectArg, _typedSourceObjectVar.Type)),
                    Expression.Assign(
                        _resultVar,
                        Expression.New(
                            Constructor,
                            from p in Parameters select ResolveArgument(p, properties)))),
                _sourceObjectArg, _predicateReflectorArg, _childProcessorArg);
        }

        Expression ResolveArgument(ParameterInfo parameter, PropertyInfo[] properties)
        {
            if (LazyChildren.TryGetLazyChildrenInterface(parameter.ParameterType) != null)
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

        /// <summary>
        /// childProcessor(lazyChildren, result, childDataProvider)
        /// </summary>
        Expression ResolveArgumentForLazyChildren(ParameterInfo parameter)
        {
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

        /// <summary>
        /// serviceProvider.GetService(typeof(parameterType))
        /// </summary>
        Expression ResolveArgumentFromServiceProvider(ParameterInfo parameter)
        {
            return Expression.Convert(
                Expression.Call(
                    Expression.Constant(_serviceProvider),
                    _serviceProviderGetServiceMethod,
                    Expression.Constant(parameter.ParameterType)),
                parameter.ParameterType);
        }

        /// <summary>
        /// predicate.ProcessArgument(propertyName, @object)
        /// </summary>
        Expression ResolveArgumentFromReflector(ParameterInfo parameter, PropertyInfo property)
        {
            return Expression.Call(
                _predicateReflectorArg,
                PredicateReflector.ProcessArgumentMethod.MakeGenericMethod(parameter.ParameterType),
                Expression.Constant(property.Name),
                Expression.Property(_typedSourceObjectVar, property));
        }
    }
}
