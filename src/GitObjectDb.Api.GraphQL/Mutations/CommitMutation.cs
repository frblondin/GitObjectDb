using GitObjectDb.Api.GraphQL.Graph;
using GraphQL;
using GraphQL.Resolvers;
using LibGit2Sharp;

namespace GitObjectDb.Api.GraphQL.Mutations;

internal class CommitMutation : IFieldResolver
{
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context)
    {
        var mutationContext = MutationContext.GetCurrent(context);
        try
        {
            var message = context.GetArgument<string>(Mutation.MessageArgument);
            var signature = CreateSignature(context);
            var commit = mutationContext.Transformations.Commit(new(message, signature, signature));
            return ValueTask.FromResult((object?)commit.Id);
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
        return new Signature(new Identity(name, email), DateTimeOffset.Now);
    }
}
