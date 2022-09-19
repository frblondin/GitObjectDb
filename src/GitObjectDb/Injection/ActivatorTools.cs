using GitObjectDb.Injection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Helper code for the various activator services.
/// </summary>
internal static class ActivatorTools
{
    /// <summary>
    /// Finds the preferred constructor accessible in the <paramref name="instanceType"/>.
    /// </summary>
    /// <param name="instanceType">Type of the instance.</param>
    /// <param name="argumentTypes">The argument types.</param>
    /// <param name="parameterMap">The parameter map.</param>
    /// <returns>The preferred constructor found using reflection.</returns>
    internal static ConstructorInfo FindPreferredConstructor(Type instanceType, Type[] argumentTypes, out int?[] parameterMap)
    {
        foreach (var declaredConstructor in instanceType.GetTypeInfo().DeclaredConstructors)
        {
            if (!declaredConstructor.IsStatic && declaredConstructor.IsDefined(typeof(FactoryDelegateConstructorAttribute), false))
            {
                if (!TryCreateParameterMap(declaredConstructor.GetParameters(), argumentTypes, out int?[] parameterMap2))
                {
                    ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                }
                parameterMap = parameterMap2;
                return declaredConstructor;
            }
        }
        throw new InvalidOperationException($"A suitable constructor for type '{instanceType.Name}' could not be located. Ensure the type is concrete and services are registered for all parameters of a public constructor decorated with {nameof(FactoryDelegateConstructorAttribute)}.");
    }

    private static bool TryCreateParameterMap(ParameterInfo[] constructorParameters, Type[] argumentTypes, out int?[] parameterMap)
    {
        parameterMap = new int?[constructorParameters.Length];
        for (int i = 0; i < argumentTypes.Length; i++)
        {
            var flag = false;
            var typeInfo = argumentTypes[i].GetTypeInfo();
            for (var j = 0; j < constructorParameters.Length; j++)
            {
                if (!parameterMap[j].HasValue && constructorParameters[j].ParameterType.GetTypeInfo().IsAssignableFrom(typeInfo))
                {
                    flag = true;
                    parameterMap[j] = i;
                    break;
                }
            }
            if (!flag)
            {
                return false;
            }
        }
        return true;
    }

    [ExcludeFromCodeCoverage]
    private static void ThrowMarkedCtorDoesNotTakeAllProvidedArguments()
    {
        throw new InvalidOperationException($"Constructor marked with {nameof(FactoryDelegateConstructorAttribute)} does not accept all given argument types.");
    }
}
