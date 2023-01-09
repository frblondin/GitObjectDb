using Fasterflect;
using GitObjectDb.Api.ProtoBuf.Model;
using GitObjectDb.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.ProtoBuf;

/// <summary>A set of methods for instances of <see cref="IEndpointRouteBuilder"/>.</summary>
public static class IEndpointRouteBuilderExtensions
{
    /// <summary>Adds support of protobuf queries.</summary>
    /// <param name="source">The source.</param>
    /// <returns>The source <see cref="IEndpointRouteBuilder"/>.</returns>
    public static IEndpointRouteBuilder AddGitObjectProtobufControllers(this IEndpointRouteBuilder source)
    {
        source.MapGrpcServicesForModel();
        source.ServiceProvider.ConfigureGitObjectDbProtoRuntimeTypeModel();
        return source;
    }

    private static void MapGrpcServicesForModel(this IEndpointRouteBuilder source)
    {
        var model = source.ServiceProvider.GetRequiredService<IDataModel>();
        foreach (var nodeType in model.NodeTypes)
        {
            source.MapGrpcService(nodeType);
        }
    }

    private static void MapGrpcService(this IEndpointRouteBuilder source, NodeTypeDescription nodeType)
    {
        var serviceType = typeof(NodeQueryService<>).MakeGenericType(nodeType.Type);
        var method = Reflect.Method(typeof(GrpcEndpointRouteBuilderExtensions),
                                    nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService),
                                    new[] { serviceType },
                                    typeof(IEndpointRouteBuilder));
        method.Invoke(null, source);
    }
}
