using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GitObjectDb.Api.ProtoBuf.Model;

/// <summary>Provides methods for querying a GitObjectDb repository.</summary>
/// <typeparam name="TNode">The type of the <see cref="Node"/>.</typeparam>
[ServiceContract]
public interface INodeQueryService<TNode>
    where TNode : Node
{
    /// <summary>Gets nodes from repository.</summary>
    /// <param name="request">The description of the request.</param>
    /// <param name="context">The context to be shared with service implementation.</param>
    /// <returns>The <see cref="NodeQueryReply{TNode}"/>.</returns>
    [OperationContract]
    Task<NodeQueryReply<TNode>> QueryNodesAsync(NodeQueryRequest request, CallContext context = default);

    /// <summary>Gets node changes from repository between two committish.</summary>
    /// <param name="request">The description of the request.</param>
    /// <param name="context">The context to be shared with service implementation.</param>
    /// <returns>The <see cref="NodeQueryReply{TNode}"/>.</returns>
    [OperationContract]
    Task<NodeDeltaQueryReply<TNode>> QueryNodeDeltasAsync(NodeDeltaQueryRequest request, CallContext context = default);
}