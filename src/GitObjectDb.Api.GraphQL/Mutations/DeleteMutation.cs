using GitObjectDb.Api.GraphQL.Graph;
using GraphQL;
using GraphQL.Resolvers;

namespace GitObjectDb.Api.GraphQL.Mutations;

internal class DeleteMutation : IFieldResolver
{
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        var mutationContext = MutationContext.GetCurrent(context);

        try
        {
            var path = context.GetArgument<DataPath>(Mutation.PathArgument);
            mutationContext.Transformations.Revert(path);

            return ValueTask.FromResult((object?)path);
        }
        catch
        {
            mutationContext.AnyException |= true;
            throw;
        }
    }
}
