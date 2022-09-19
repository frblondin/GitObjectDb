using AutoMapper;
using GraphQL;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL;

internal static class GraphQLHelper
{
    public static FuncFieldResolver<object?, object?> CreateFieldResolver<TSource>(Func<TSource?, IResolveFieldContext, DataProvider, IMapper, object?> resolver)
        where TSource : class =>
        new(context =>
            {
                var source = context.Source as TSource;
                var provider = context.RequestServices?.GetRequiredService<DataProvider>() ??
                    throw new NotSupportedException("No request context set.");
                var mapper = context.RequestServices?.GetRequiredService<IMapper>() ??
                   throw new NotSupportedException("No mapper context set.");
                return resolver(source, context, provider, mapper);
            });
}