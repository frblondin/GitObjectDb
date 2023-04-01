using GitObjectDb.Api.OData.Model;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace GitObjectDb.Api.OData;

public class NodeController<TNode, TNodeDTO> : ODataController
    where TNode : Node
    where TNodeDTO : NodeDto
{
    private readonly DataTransferTypeDescription _description;

    internal NodeController(DataProvider dataProvider, DtoTypeEmitter typeEmitter)
    {
        DataProvider = dataProvider;
        _description = typeEmitter.TypeDescriptions.Single(d => d.NodeType.Type == typeof(TNode));
    }

    public DataProvider DataProvider { get; }

    [EnableQuery]
    public IEnumerable<TNodeDTO> Get([FromODataUri] string committish,
                                     [FromODataUri] string? parentPath = null,
                                     [FromODataUri] bool isRecursive = false)
    {
        return DataProvider
            .GetNodes<TNode>(_description, committish, parentPath, isRecursive)
            .Cast<TNodeDTO>();
    }
}
