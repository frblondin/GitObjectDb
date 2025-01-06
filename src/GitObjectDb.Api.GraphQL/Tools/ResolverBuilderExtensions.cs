using GraphQL.Builders;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Tools;

internal static class ResolverBuilderExtensions
{
    internal static DIResolverBuilder<TSource, TReturn> ResolveThroughDI<TSource, TReturn>(this FieldBuilder<TSource, TReturn> builder)
    {
        return new DIResolverBuilder<TSource, TReturn>(builder);
    }
}

#pragma warning disable SA1402 // File may only contain a single type
internal class DIResolverBuilder<TSource, TReturn>(FieldBuilder<TSource, TReturn> builder)
{
    public FieldBuilder<TSource, TReturn> UsingLoader<TKey, TDataLoader>()
        where TKey : notnull
        where TDataLoader : DataLoaderBase<TKey, TReturn>
    {
        builder.ResolveAsync(async context =>
        {
            var loader = context.RequestServices!.GetRequiredService<TDataLoader>();
            var key = (TKey)Activator.CreateInstance(typeof(TKey), context)!;
            return await loader.LoadAsync(key).GetResultAsync();
        });
        return builder;
    }

    public FieldBuilder<TSource, TReturn> UsingLoader<TKey>(Type type)
        where TKey : notnull
    {
        builder.ResolveAsync(async context =>
        {
            var loader = (DataLoaderBase<TKey, TReturn>)context.RequestServices!.GetRequiredService(type);
            var key = (TKey)Activator.CreateInstance(typeof(TKey), context)!;
            return await loader.LoadAsync(key).GetResultAsync();
        });
        return builder;
    }

    internal FieldBuilder<TSource, TReturn> UsingResolver<TFieldResolver>()
        where TFieldResolver : class, IFieldResolver
    {
        return builder.ResolveAsync(async context =>
        {
            var resolver = context.RequestServices?.GetRequiredService<TFieldResolver>() ??
                throw new RequestError("No request services set.");
            return (TReturn?)await resolver.ResolveAsync(context);
        });
    }
}