using AutoMapper;
using GitObjectDb.Api.Model;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL.Commands;
internal static partial class NodeMutation
{
    internal sealed class Context
    {
        internal Context(IServiceProvider serviceProvider)
        {
            Connection = serviceProvider.GetRequiredService<IConnection>();
            DataProvider = serviceProvider.GetRequiredService<DataProvider>();
            Mapper = serviceProvider.GetRequiredService<IMapper>();
            Transformations = Connection.Update();
        }

        internal static AsyncLocal<NodeMutation.Context?> Current { get; } = new();

        internal IConnection Connection { get; }

        internal DataProvider DataProvider { get; }

        internal IMapper Mapper { get; }

        internal ITransformationComposer Transformations { get; }

        internal IDictionary<DataPath, Node> ModifiedNodesByPath { get; } = new Dictionary<DataPath, Node>();

        internal IDictionary<UniqueId, Node> ModifiedNodesById { get; } = new Dictionary<UniqueId, Node>();

        internal bool AnyException { get; set; }

        internal void ThrowIfAnyException()
        {
            if (AnyException)
            {
                throw new GitObjectDbException("A previous exception occurred.");
            }
        }

        internal NodeDto Convert(DataPath path)
        {
            var node = TryResolve(path) ??
                throw new GitObjectDbException($"The node '{path}' could not be found.");
            var commitId = Connection.Repository.Info.IsHeadUnborn ? ObjectId.Zero : Connection.Repository.Head.Tip.Id;
            return DataProvider.Map<Node, NodeDto>(node, commitId)!;
        }

        internal Node? TryResolve(DataPath path)
        {
            if (ModifiedNodesByPath.TryGetValue(path, out var node))
            {
                return node;
            }
            return Connection.Repository.Info.IsHeadUnborn ?
                null :
                Connection.Lookup<Node>(path);
        }

        internal Node? TryResolve(UniqueId id)
        {
            if (ModifiedNodesById.TryGetValue(id, out var node))
            {
                return node;
            }
            return Connection.Repository.Info.IsHeadUnborn ?
                null :
                Connection.Lookup<Node>(id);
        }
    }
}
