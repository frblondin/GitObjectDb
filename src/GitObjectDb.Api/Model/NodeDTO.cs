namespace GitObjectDb.Api.Model;

public class NodeDto
{
    protected NodeDto(Node node)
    {
        Node = node;
    }

    public Node Node { get; }

    public string? Id { get; set; }

    public string? Path { get; set; }

    public IEnumerable<NodeDto> Children =>
        ChildResolver?.Invoke() ?? Enumerable.Empty<NodeDto>();

    internal Func<IEnumerable<NodeDto>>? ChildResolver { get; set; }
}
