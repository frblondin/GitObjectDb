using GitObjectDb.Attributes;
using GitObjectDb.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for adding services to an <see cref="IServiceCollection" />.
    /// </summary>
    [ExcludeFromGuardForNull]
    public static class FactoryDelegateProviderExtensions
    {
        static readonly MethodInfo _getRequiredServiceDefinition = ExpressionReflector.GetMethod(() => ServiceProviderServiceExtensions.GetRequiredService<object>(null), true);

        /// <summary>
        /// Adds a factory delefate that returns a new instance of the type specified by the <typeparamref name="TDelegate"/> delegate.
        /// </summary>
        /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection" /> to add the service to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddFactoryDelegate<TDelegate, TImplementation>(this IServiceCollection services)
            where TDelegate : Delegate
            where TImplementation : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var invoker = CreateInvoker<TDelegate>(typeof(TImplementation));
            return services.AddSingleton(invoker.Compile());
        }

        static Expression<Func<IServiceProvider, TDelegate>> CreateInvoker<TDelegate>(Type implementationType)
        {
            var capturedServiceProvider = Expression.Parameter(typeof(IServiceProvider), "capturedServiceProvider");
            var factory = CreateFactory<TDelegate>(capturedServiceProvider, implementationType);

            // capturedServiceProvider =>
            //    (param1, param2...) =>
            //       new Implemenation(capturedServiceProvider.GetRequiredService<XXX>(),
            //                         param1,
            //                         param2,
            //                         ...)
            return Expression.Lambda<Func<IServiceProvider, TDelegate>>(factory, capturedServiceProvider);
        }

        static Expression<TDelegate> CreateFactory<TDelegate>(ParameterExpression capturedServiceProvider, Type implementationType)
        {
            var (parameters, argumentTypes, returnType) = GetDelegateData<TDelegate>();
            var constructor = ActivatorTools.FindPreferredConstructor(implementationType, argumentTypes, out var map);
            var invokeConstructor = Expression.Convert(
                Expression.New(constructor, from p in constructor.GetParameters()
                                            select MapParameter(p, map, parameters, capturedServiceProvider)),
                returnType);
            return Expression.Lambda<TDelegate>(invokeConstructor, parameters);
        }

        static (IList<ParameterExpression> parameters, Type[] argumentTypes, Type ReturnType) GetDelegateData<TDelegate>()
        {
            var invokeMethod = typeof(TDelegate).GetMethod("Invoke");
            return ((from p in invokeMethod.GetParameters()
                     select Expression.Parameter(p.ParameterType, p.Name)).ToList(),
                    (from p in invokeMethod.GetParameters()
                     select p.ParameterType).ToArray(),
                    invokeMethod.ReturnType);
        }

        static Expression MapParameter(ParameterInfo p, int?[] map, IList<ParameterExpression> delegateArgs, Expression serviceProvider)
        {
            if (map[p.Position] != null)
            {
                return delegateArgs[(int)map[p.Position]];
            }
            else
            {
                var getRequiredServiceMethod = _getRequiredServiceDefinition.MakeGenericMethod(p.ParameterType);
                return Expression.Call(getRequiredServiceMethod, serviceProvider);
            }
        }
    }
}
