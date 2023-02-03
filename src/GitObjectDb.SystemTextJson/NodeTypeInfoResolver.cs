using Fasterflect;
using GitObjectDb.Model;
using GitObjectDb.Tools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using static System.Linq.Expressions.Expression;

namespace GitObjectDb.SystemTextJson;

internal class NodeTypeInfoResolver : DefaultJsonTypeInfoResolver
{
    public NodeTypeInfoResolver(IDataModel model)
    {
        Model = model;
    }

    public IDataModel Model { get; }

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var result = base.GetTypeInfo(type, options);

        if (typeof(Node).IsAssignableFrom(type))
        {
            var description = Model.GetDescription(type);
            ExcludeIgnoreNonSerializedProperties(description, result);
            AddPolymorphismOptions(type, result);
            AddSupportOfDependencyInjection(type, result);
        }

        return result;
    }

    private static void ExcludeIgnoreNonSerializedProperties(NodeTypeDescription typeDescription, JsonTypeInfo jsonTypeInfo)
    {
        foreach (var property in jsonTypeInfo.Properties)
        {
            var serializable = typeDescription.SerializableProperties.FirstOrDefault(
                p => p.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
            if (serializable is null)
            {
                property.ShouldSerialize = (_, _) => false;
            }
        }
    }

    private void AddPolymorphismOptions(Type type, JsonTypeInfo jsonTypeInfo)
    {
        jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
        {
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
        };
        AddDerivedTypes(type, jsonTypeInfo.PolymorphismOptions.DerivedTypes);
    }

    private void AddDerivedTypes(Type nodeType, ICollection<JsonDerivedType> derivedTypes)
    {
        foreach (var type in from description in Model.NodeTypes
                             let modelType = description.Type
                             where nodeType.IsAssignableFrom(modelType)
                             where !modelType.IsAbstract
                             select modelType)
        {
            derivedTypes.Add(new(type, type.FullName));
        }
    }

    private static void AddSupportOfDependencyInjection(Type type, JsonTypeInfo result)
    {
        if (type.IsAbstract)
        {
            return;
        }
        var parameterlessConstructor = type.GetConstructor(Type.EmptyTypes);
        if (parameterlessConstructor is null)
        {
            var greediestConstructor = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                .Where(c => c.GetParameters().All(IsInjectable))
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            bool IsInjectable(ParameterInfo parameter) =>
                !parameter.ParameterType.IsNode() &&
                !parameter.ParameterType.IsNodeEnumerable(out var _);

            if (greediestConstructor is not null)
            {
                result.CreateObject = CreateFactoryUsingDI(greediestConstructor).Compile();
            }
        }
    }

    private static Expression<Func<object>> CreateFactoryUsingDI(ConstructorInfo constructor)
    {
        var serviceProviderProperty = typeof(NodeSerializerContext)
            .GetProperty(nameof(NodeSerializerContext.ServiceProvider), BindingFlags.Public | BindingFlags.Static);
        var getRequiredServiceMethod = ExpressionReflector.GetMethod(
            () => ServiceProviderServiceExtensions.GetRequiredService<object>(default!),
            returnGenericDefinition: true);
        var getServiceMethod = ExpressionReflector.GetMethod(
            () => ServiceProviderServiceExtensions.GetService<object>(default!),
            returnGenericDefinition: true);
        var serviceProviderVar = Variable(typeof(IServiceProvider));
        return Lambda<Func<object>>(
            Block(
                new[] { serviceProviderVar },
                new List<Expression>
                {
                    Assign(serviceProviderVar, Property(null, serviceProviderProperty)),
                    New(
                        constructor,
                        constructor.GetParameters().Select(ResolveConstructorArgument)),
                }));

        Expression ResolveConstructorArgument(ParameterInfo parameter) =>
            parameter.ParameterType == typeof(IServiceProvider) ?
            serviceProviderVar :
            ResolveConstructorDIArgument(parameter);

        Expression ResolveConstructorDIArgument(ParameterInfo parameter) =>
            parameter.GetCustomAttribute<OptionalAttribute>() is null ?
            Call(null, getRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), serviceProviderVar) :
            Call(null, getServiceMethod.MakeGenericMethod(parameter.ParameterType), serviceProviderVar);
    }
}
