using GitObjectDb.Api.GraphQL.Graph;
using GraphQL;
using GraphQL.Resolvers;

namespace GitObjectDb.Api.GraphQL.Mutations;

internal class DeleteMutation : IFieldResolver
{
    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        var mutationContext = MutationContext.GetCurrent(context);

        try
        {
            var path = context.GetArgument<DataPath>(Mutation.PathArgument);
            var transformations = await mutationContext.GetTransformationsAsync();
            await transformations.RevertAsync(path);

            return path;
        }
        catch
        {
            mutationContext.AnyException |= true;
            throw;
        }
    }
}
