namespace GitObjectDb.Api.Model;

public abstract class NodeDTO
{
    protected NodeDTO(Node node)
    {
        Node = node;
    }

    public Node Node { get; }

    public string? Id { get; set; }

    public string? Path { get; set; }

    public IEnumerable<NodeDTO> Children =>
        ChildResolver?.Invoke() ?? Enumerable.Empty<NodeDTO>();

    internal Func<IEnumerable<NodeDTO>>? ChildResolver { get; set; }
}
