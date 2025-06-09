using GitDotNet;
using GitObjectDb.Api.GraphQL.Graph;
using GraphQL;
using GraphQL.Execution;
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

    internal static async Task<CommitEntry> GetCommitAsync(this IResolveFieldContext context, string argumentName = Query.CommittishArgument)
    {
        var connection = context.RequestServices?.GetRequiredService<IConnection>() as IConnection ??
            throw new ExecutionError("No connection found to resolve commit for key.");
        var id = await GetCommitIdAsync(context);
        return await connection.Repository.Objects.GetAsync<CommitEntry>(id);
    }

    internal static async Task<HashId> GetCommitIdAsync(this IResolveFieldContext context, string argumentName = Query.CommittishArgument)
    {
        var value = default(object);
        var parentContext = FindArgumentInParentContexts(context, argumentName, ref value);
        var result = value switch
        {
            HashId id => id,
            _ => await parentContext.ComputeCommitIdAsync(value?.ToString() ?? "main"),
        };
        parentContext.Arguments![argumentName] = new ArgumentValue(result, ArgumentSource.Literal);
        return result;
    }

    private static IResolveFieldContext FindArgumentInParentContexts(IResolveFieldContext context, string argumentName, ref object? value)
    {
        var parentContext = context;
        while (parentContext is not null)
        {
            if (parentContext.Arguments is not null && parentContext.Arguments.TryGetValue(argumentName, out var argValue))
            {
#pragma warning disable S3236 // Caller information arguments should not be provided explicitly
                ArgumentNullException.ThrowIfNull(argValue.Value, argumentName);
#pragma warning restore S3236 // Caller information arguments should not be provided explicitly
                value = argValue.Value;
                break;
            }
            parentContext = parentContext.Parent;
        }
        if (parentContext?.Arguments is null)
        {
            throw new InvalidOperationException("Parent context arguments is not initialized.");
        }

        return parentContext;
    }

    private static async Task<HashId> ComputeCommitIdAsync(this IResolveFieldContext context, string committish)
    {
        if (HashId.TryParse(committish, out var id))
        {
            return id;
        }
        else
        {
            var connection = context.RequestServices?.GetRequiredService<IConnection>() as IConnection ??
                throw new ExecutionError("No connection found to resolve commit for key.");
            var commit = await connection.Repository.GetCommittishAsync(committish);
            if (commit is null)
            {
                throw new GitObjectDbInvalidCommitException();
            }
            return commit.Id;
        }
    }
}
