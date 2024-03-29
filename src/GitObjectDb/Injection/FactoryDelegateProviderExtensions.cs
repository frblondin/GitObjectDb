using GitObjectDb.Injection;
using GitObjectDb.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for adding services to an <see cref="IServiceCollection" />.</summary>
public static class FactoryDelegateProviderExtensions
{
    private static readonly MethodInfo _getRequiredServiceDefinition = ExpressionReflector.GetMethod(
        (IServiceProvider provider) => ServiceProviderServiceExtensions.GetRequiredService<object>(provider), true);

    internal static IServiceCollection AddServicesImplementing(this IServiceCollection services,
                                                               Assembly assembly,
                                                               Type interfaceType,
                                                               ServiceLifetime lifetime)
    {
        var query = from implementationType in assembly.GetTypes()
                    where implementationType.IsClass
                    from serviceType in implementationType.GetInterfaces()
                    where interfaceType.IsGenericTypeDefinition && serviceType.IsGenericType ?
                          serviceType.GetGenericTypeDefinition() == interfaceType :
                          serviceType == interfaceType
                    select (serviceType, implementationType);
        foreach (var (serviceType, implementationType) in query)
        {
            var description = new ServiceDescriptor(serviceType, implementationType, lifetime);
            services.Add(description);
        }
        return services;
    }

    /// <summary>
    /// Adds a factory delegate that returns a new instance of the type specified by the delegate.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the service to.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="delegateTypes">The delegate types to look for factory for.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddFactoryDelegates(this IServiceCollection services,
                                                         Assembly assembly,
                                                         IEnumerable<Type> delegateTypes)
    {
        foreach (var delegateType in delegateTypes)
        {
            services.AddFactoryDelegate(delegateType, assembly);
        }
        return services;
    }

    /// <summary>
    /// Adds a factory delegate that returns a new instance of the type specified by the <typeparamref name="TDelegate"/>
    /// delegate.
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the service to.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddFactoryDelegate<TDelegate>(this IServiceCollection services,
                                                                   Assembly assembly)
        where TDelegate : Delegate
    {
        return services.AddFactoryDelegate(typeof(TDelegate), assembly);
    }

    private static IServiceCollection AddFactoryDelegate(this IServiceCollection services,
                                                         Type delegateType,
                                                         Assembly assembly)
    {
        var implementationType = assembly.GetTypes().FirstOrDefault(IsTypeCompatible) ??
            throw new EntryPointNotFoundException($"No implementation found for factory '{delegateType}'. " +
            $"Make sure the constructor is decorated with the {nameof(FactoryDelegateConstructorAttribute)} attribute" +
            $" and that the constructor is public.");
        var invoker = CreateInvoker(implementationType, delegateType);
        return services.AddSingleton(delegateType, (Func<IServiceProvider, object>)invoker.Compile());

        bool IsTypeCompatible(Type type) =>
            delegateType.GetMethod("Invoke")!.ReturnType.IsAssignableFrom(type) &&
            type.GetTypeInfo().DeclaredConstructors.Any(IsDecoratedWithFactoryDelegateConstructorAttribute);

        bool IsDecoratedWithFactoryDelegateConstructorAttribute(ConstructorInfo constructor) =>
            constructor.GetCustomAttribute<FactoryDelegateConstructorAttribute>()?.DelegateType == delegateType;
    }

    /// <summary>
    /// Adds a factory delegate that returns a new instance of the type specified by the <typeparamref name="TDelegate"/>
    /// delegate.
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddFactoryDelegate<TDelegate, TImplementation>(this IServiceCollection services)
        where TDelegate : Delegate
        where TImplementation : class
    {
        var invoker = CreateInvoker(typeof(TImplementation), typeof(TDelegate));
        return services.AddSingleton(typeof(TDelegate), (Func<IServiceProvider, object>)invoker.Compile());
    }

    private static LambdaExpression CreateInvoker(Type implementationType,
                                                  Type delegateType)
    {
        var capturedServiceProvider = Expression.Parameter(typeof(IServiceProvider), "capturedServiceProvider");
        var factory = CreateFactory(capturedServiceProvider, implementationType, delegateType);
        var invokerType = Expression.GetFuncType(typeof(IServiceProvider), delegateType);

        // capturedServiceProvider =>
        //    (param1, param2...) =>
        //       new Implemenation(capturedServiceProvider.GetRequiredService<XXX>(),
        //                         param1,
        //                         param2,
        //                         ...)
        return Expression.Lambda(invokerType, factory, capturedServiceProvider);
    }

    private static LambdaExpression CreateFactory(Expression capturedServiceProvider,
                                                  Type implementationType,
                                                  Type delegateType)
    {
        var (parameters, argumentTypes, returnType) = GetDelegateData(delegateType);
        var constructor = ActivatorTools.FindPreferredConstructor(implementationType, argumentTypes, out var map);
        var invokeConstructor = Expression.Convert(
            Expression.New(constructor, from p in constructor.GetParameters()
                                        select MapParameter(p, map, parameters, capturedServiceProvider)),
            returnType);
        return Expression.Lambda(delegateType, invokeConstructor, parameters);
    }

    private static (IList<ParameterExpression> Parameters, Type[] ArgumentTypes, Type ReturnType) GetDelegateData(Type delegateType)
    {
        var invokeMethod = delegateType.GetMethod("Invoke")!;
        return ((from p in invokeMethod.GetParameters()
                 select Expression.Parameter(p.ParameterType, p.Name)).ToList(),
                (from p in invokeMethod.GetParameters()
                 select p.ParameterType).ToArray(),
                invokeMethod.ReturnType);
    }

    private static Expression MapParameter(ParameterInfo p,
                                           int?[] map,
                                           IList<ParameterExpression> delegateArgs,
                                           Expression serviceProvider)
    {
        var mapped = map[p.Position];
        if (mapped.HasValue)
        {
            return delegateArgs[mapped.Value];
        }
        else if (p.ParameterType == typeof(IServiceProvider))
        {
            return serviceProvider;
        }
        else
        {
            var getRequiredServiceMethod = _getRequiredServiceDefinition.MakeGenericMethod(p.ParameterType);
            return Expression.Call(getRequiredServiceMethod, serviceProvider);
        }
    }
}
