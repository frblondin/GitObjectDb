using AutoMapper;
using Fasterflect;
using GitObjectDb.Api.Model;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace GitObjectDb.Api.GraphQL.Queries;
internal static class NodeReferenceQuery
{
    internal static FuncFieldResolver<object?, object?> CreateSingleReferenceResolver(Type nodeType, PropertyInfo property)
    {
        return new(Execute);

        object Execute(IResolveFieldContext<object?> context)
        {
            var parentNode = (context.Source as NodeDto)?.Node ??
                throw new RequestError("Could not get parent node.");
            var getter = Reflect.PropertyGetter(parentNode.GetType(), property.Name);
            var reference = (Node)getter.Invoke(parentNode);
            var mapper = context.RequestServices?.GetRequiredService<IMapper>() ??
                throw new ExecutionError("No mapper context set.");
            return mapper.Map(reference, nodeType, property.PropertyType);
        }
    }

    internal static FuncFieldResolver<object?, object?> CreateMultiReferenceResolver(Type dtoType, Type nodeType, MemberInfo member)
    {
        var sourceEnumType = typeof(IEnumerable<>).MakeGenericType(nodeType);
        var destEnumType = typeof(IEnumerable<>).MakeGenericType(dtoType);
        return new(Execute);

        object Execute(IResolveFieldContext<object?> context)
        {
            var parentNode = (context.Source as NodeDto)?.Node ??
                throw new RequestError("Could not get parent node.");
            var getter = Reflect.PropertyGetter(parentNode.GetType(), member.Name);
            var references = (Node)getter.Invoke(parentNode);
            var mapper = context.RequestServices?.GetRequiredService<IMapper>() ??
                throw new ExecutionError("No mapper context set.");
            return mapper.Map(references,
                              sourceEnumType,
                              destEnumType);
        }
    }
}
