using GitObjectDb.Api.Model;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace GitObjectDb.Api.OData;

public class NodeController<TNode, TNodeDTO> : ODataController
    where TNode : Node
    where TNodeDTO : NodeDto
{
    private readonly DataProvider _dataProvider;

    protected NodeController(DataProvider dataProvider)
    {
        _dataProvider = dataProvider;
    }

    [EnableQuery]
    public IEnumerable<TNodeDTO> Get([FromODataUri] string? parentPath = null,
                                     [FromODataUri] string? committish = null,
                                     [FromODataUri] bool isRecursive = false)
    {
        return _dataProvider.GetNodes<TNode, TNodeDTO>(parentPath, committish, isRecursive);
    }
}
