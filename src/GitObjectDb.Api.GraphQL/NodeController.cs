using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace GitObjectDb.Api.GraphQL;

[ApiController]
[Route("api")]
public class NodeController : Controller
{
    private readonly IDocumentExecuter _documentExecuter;
    private readonly ISchema _schema;
    private readonly IGraphQLTextSerializer _serializer;

    public NodeController(IDocumentExecuter documentExecuter, ISchema schema, IGraphQLTextSerializer serializer)
    {
        _documentExecuter = documentExecuter;
        _schema = schema;
        _serializer = serializer;
    }

    [HttpPost("graphql")]
    public async Task<IActionResult> GraphQL([FromBody] JsonObject content)
    {
        var request = _serializer.Deserialize<GraphQLRequest>(content.ToJsonString())!;
        var result = await _documentExecuter.ExecuteAsync(s =>
        {
            s.Schema = _schema;
            s.Query = request.Query;
            s.Variables = request.Variables;
            s.OperationName = request.OperationName;
            s.RequestServices = HttpContext.RequestServices;
            s.UserContext = new GraphQLUserContext
            {
                User = HttpContext.User,
            };
            s.CancellationToken = HttpContext.RequestAborted;
        });

        return new ExecutionResultActionResult(result);
    }
}
