using GitObjectDb.Api.GraphQL.Graph;
using GraphQL;
using GraphQL.Execution;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Tools;
internal static class IResolveFieldContextExtensions
{
    internal static TType GetArgumentFromParentContexts<TType>(this IResolveFieldContext context, string name, TType defaultValue = default!)
    {
        var parentContext = context;
        while (parentContext is not null)
        {
            var value = context.GetArgument<TType?>(name);
            if (value is not null)
            {
                return value;
            }
            parentContext = parentContext.Parent;
        }
        return defaultValue;
    }

    internal static ObjectId GetCommitId(this IResolveFieldContext context, string argumentName = Query.CommittishArgument)
    {
        var parentContext = context;
        ObjectId? result = default;
        while (parentContext is not null)
        {
            if (parentContext.Arguments is not null && parentContext.Arguments.TryGetValue(argumentName, out var argValue))
            {
                if (argValue.Value is ObjectId id)
                {
                    return id;
                }
                var value = (string?)argValue.Value;
                result = parentContext.ComputeCommitId(value);
                break;
            }

            if (parentContext.Parent is null)
            {
                break;
            }
            parentContext = parentContext.Parent;
        }
        if (parentContext!.Arguments is null)
        {
            throw new InvalidOperationException("Parent context arguments is not initialized.");
        }
        result ??= parentContext.ComputeCommitId(default);
        parentContext.Arguments[argumentName] = new ArgumentValue(result, ArgumentSource.Literal);
        return result;
    }

    private static ObjectId ComputeCommitId(this IResolveFieldContext context, string? committish)
    {
        if (ObjectId.TryParse(committish, out var id))
        {
            return id;
        }
        else
        {
            var connection = context.RequestServices?.GetRequiredService<IQueryAccessor>() as IConnection ??
                throw new ExecutionError("No connection found to resolve commit for key.");
            if (connection.Repository.Info.IsHeadUnborn)
            {
                throw new ExecutionError("Repository is empty.");
            }
            if (committish is not null)
            {
                var commit = (Commit)connection.Repository.Lookup(committish);
                if (commit is null)
                {
                    throw new GitObjectDbInvalidCommitException();
                }
                return commit.Id;
            }
            else
            {
                return connection.Repository.Head.Tip.Id;
            }
        }
    }
}
