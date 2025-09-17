using GitDotNet;
using GitObjectDb.Api.GraphQL.Graph;
using GraphQL;
using GraphQL.Resolvers;

namespace GitObjectDb.Api.GraphQL.Mutations;

internal class CommitMutation : IFieldResolver
{
    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        var mutationContext = MutationContext.GetCurrent(context);
        try
        {
            var message = context.GetArgument<string>(Mutation.MessageArgument);
            var signature = CreateSignature(context);
            var transformations = await mutationContext.GetTransformationsAsync();
            var commit = await transformations.CommitAsync(new(message, signature, signature));
            return commit.Id;
        }
        finally
        {
            mutationContext.Reset();
        }
    }

    private static Signature CreateSignature(IResolveFieldContext context)
    {
        var name = context.GetArgument<string>(Mutation.AuthorArgument);
        var email = context.GetArgument<string>(Mutation.EMailArgument);
        return new Signature(name, email, DateTimeOffset.Now);
    }
}
