using LibGit2Sharp;

namespace GitObjectDb.Api.OData.Model;

/// <summary>Data transfer objects used to provide information about <see cref="Node"/> instances.</summary>
public class NodeDto
{
    /// <summary>Initializes a new instance of the <see cref="NodeDto"/> class.</summary>
    /// <param name="node">The original <see cref="Node"/> instance that the data transfer object represents.</param>
    /// <param name="commitId">The commit id that the <paramref name="node"/> has been retrieved from.</param>
    protected NodeDto(Node? node, ObjectId? commitId)
    {
        Node = node;
        Id = node?.Id.ToString() ?? UniqueId.CreateNew().ToString();
        CommitId = commitId?.Sha;
    }

    /// <summary>Gets the original <see cref="Node"/> instance that the data transfer object represents.</summary>
    public Node? Node { get; }

    /// <summary>Gets the commit containing this node.</summary>
    public string? CommitId { get; }

    /// <summary>Gets the node unique identifier.</summary>
    public string Id { get; init; }

    /// <summary>Gets the node path.</summary>
    public string? Path { get; init; }

    /// <summary>Gets all node children.</summary>
    public IEnumerable<NodeDto> Children =>
        ChildResolver?.Invoke() ?? Enumerable.Empty<NodeDto>();

    internal Func<IEnumerable<NodeDto>>? ChildResolver { get; init; }
}
