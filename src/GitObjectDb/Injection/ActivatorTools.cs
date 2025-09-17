using GitObjectDb.Injection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

internal static class ActivatorTools
{
    internal static MethodBase FindPreferredMember(Type instanceType, Type[] argumentTypes, Type returnType, out int?[] parameterMap)
    {
        return (MethodBase?)FindPreferredConstructor(instanceType, argumentTypes, out parameterMap) ??
            FindPreferredStaticMethod(instanceType, argumentTypes, returnType, out parameterMap) ??
            throw new InvalidOperationException($"A suitable constructor or static method for type '{instanceType.Name}' could not be located. " +
                $"Ensure the type is concrete and services are registered for all parameters of a public constructor decorated " +
                $"with {nameof(FactoryDelegateAttribute)}.");
    }

    private static ConstructorInfo? FindPreferredConstructor(Type instanceType, Type[] argumentTypes, out int?[] parameterMap)
    {
        foreach (var constructor in instanceType.GetTypeInfo().DeclaredConstructors)
        {
            if (!constructor.IsStatic && constructor.IsDefined(typeof(FactoryDelegateAttribute), false))
            {
                if (!TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out int?[] parameterMap2))
                {
                    ThrowMarkedMemberDoesNotTakeAllProvidedArguments();
                }
                parameterMap = parameterMap2;
                return constructor;
            }
        }
        parameterMap = [];
        return null;
    }

    private static MethodInfo? FindPreferredStaticMethod(Type instanceType, Type[] argumentTypes, Type returnType, out int?[] parameterMap)
    {
        foreach (var method in instanceType.GetTypeInfo().DeclaredMethods)
        {
            if (method.IsStatic && method.IsDefined(typeof(FactoryDelegateAttribute), false))
            {
                if (method.ReturnType != returnType)
                {
                    ThrowStaticMethodReturnsWrongType();
                }
                if (!TryCreateParameterMap(method.GetParameters(), argumentTypes, out int?[] parameterMap2))
                {
                    ThrowMarkedMemberDoesNotTakeAllProvidedArguments();
                }
                parameterMap = parameterMap2;
                return method;
            }
        }
        parameterMap = [];
        return null;
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
    private static void ThrowMarkedMemberDoesNotTakeAllProvidedArguments()
    {
        const string message =
            $"Member marked with {nameof(FactoryDelegateAttribute)} does not " +
            $"accept all given argument types.";
        throw new InvalidOperationException(message);
    }

    [ExcludeFromCodeCoverage]
    private static void ThrowStaticMethodReturnsWrongType()
    {
        const string message =
            $"Method with {nameof(FactoryDelegateAttribute)} does not " +
            $"return expected type.";
        throw new InvalidOperationException(message);
    }
}
