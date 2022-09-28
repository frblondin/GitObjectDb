using GitObjectDb.Api.OData.Model;
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
    public IEnumerable<TNodeDTO> Get([FromODataUri] string committish,
                                     [FromODataUri] string? parentPath = null,
                                     [FromODataUri] bool isRecursive = false)
    {
        return _dataProvider.GetNodes<TNode, TNodeDTO>(committish, parentPath, isRecursive);
    }
}
