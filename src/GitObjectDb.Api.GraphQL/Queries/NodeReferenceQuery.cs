using AutoMapper;
using Fasterflect;
using GitObjectDb.Api.Model;
using GraphQL;
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
            var parentNode = context.Source as NodeDto ??
                throw new NotSupportedException("Could not get parent node.");
            var getter = Reflect.PropertyGetter(parentNode.Node.GetType(), property.Name);
            var reference = (Node)getter.Invoke(parentNode.Node);
            var mapper = context.RequestServices?.GetRequiredService<IMapper>() ??
                throw new NotSupportedException("No mapper context set.");
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
            var parentNode = context.Source as NodeDto ??
                throw new NotSupportedException("Could not get parent node.");
            var getter = Reflect.PropertyGetter(parentNode.Node.GetType(), member.Name);
            var references = (Node)getter.Invoke(parentNode.Node);
            var mapper = context.RequestServices?.GetRequiredService<IMapper>() ??
                throw new NotSupportedException("No mapper context set.");
            return mapper.Map(references,
                              sourceEnumType,
                              destEnumType);
        }
    }
}
