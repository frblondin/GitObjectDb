using GraphQL;
using GraphQL.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Mutations;

internal sealed class MutationContext
{
    private const string NodeMutationVariableName = $"${nameof(MutationContext)}";

    private string? _branchName;
    private IChangeComposerWithCommit? _transformationComposer;

    internal MutationContext(IServiceProvider serviceProvider)
    {
        Connection = serviceProvider.GetRequiredService<IConnection>();
        QueryAccessor = serviceProvider.GetRequiredService<IConnection>();
    }

    internal static AsyncLocal<MutationContext?> Current { get; } = new();

    internal IConnection Connection { get; }

    internal IConnection QueryAccessor { get; }

    internal string BranchName
    {
        get => _branchName ?? throw new RequestError("No branch name has been set. Please checkout the target branch first using the checkout instruction.");
        set
        {
            if (_branchName is not null)
            {
                throw new RequestError("The branch cannot be changed before commits have been pushed.");
            }
            _branchName = value;
        }
    }

    internal IDictionary<DataPath, Node> ModifiedNodesByPath { get; } = new Dictionary<DataPath, Node>();

    internal IDictionary<UniqueId, Node> ModifiedNodesById { get; } = new Dictionary<UniqueId, Node>();

    internal bool AnyException { get; set; }

    internal static MutationContext GetCurrent(IResolveFieldContext context)
    {
        if (!context.UserContext.TryGetValue(NodeMutationVariableName, out var existing))
        {
            var serviceProvider = context.RequestServices ??
                throw new ExecutionError("No service provider could be found.");
            existing = new MutationContext(serviceProvider);
            context.UserContext[NodeMutationVariableName] = existing;
        }

        var result = (MutationContext)existing!;
        result.ThrowIfAnyException();
        return result;
    }

    internal async Task<IChangeComposerWithCommit> GetTransformationsAsync() => _transformationComposer ??= await Connection.UpdateAsync(BranchName);

    internal void ThrowIfAnyException()
    {
        if (AnyException)
        {
            throw new GitObjectDbException("A previous exception occurred.");
        }
    }

    internal async Task<Node?> TryResolveAsync(DataPath path)
    {
        if (ModifiedNodesByPath.TryGetValue(path, out var node))
        {
            return node;
        }
        return Connection.Repository.Branches.TryGet(BranchName, out var branch) ?
            await Connection.LookupAsync<Node>(await branch.GetTipAsync(), path) :
            null;
    }

    internal async Task<Node?> TryResolveAsync(UniqueId id)
    {
        if (ModifiedNodesById.TryGetValue(id, out var node))
        {
            return node;
        }
        return Connection.Repository.Branches.TryGet(BranchName, out var branch) ?
            await Connection.LookupAsync<Node>(await branch.GetTipAsync(), id) :
            null;
    }

    internal void Reset()
    {
        _transformationComposer = null;
        ModifiedNodesByPath.Clear();
        ModifiedNodesById.Clear();
    }
}