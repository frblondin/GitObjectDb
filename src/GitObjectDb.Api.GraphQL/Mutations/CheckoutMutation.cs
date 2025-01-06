using GitObjectDb.Api.GraphQL.Graph;
using GraphQL;
using GraphQL.Resolvers;

namespace GitObjectDb.Api.GraphQL.Mutations;

internal class CheckoutMutation : IFieldResolver
{
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        var branch = context.GetArgument<string>(Mutation.BranchArgument);
        MutationContext.GetCurrent(context).BranchName = branch;
        return ValueTask.FromResult((object?)branch);
    }
}
