using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GitObjectDb.Api.GraphQL;

/// <summary>Controller for handling GraphQL requests.</summary>
/// <remarks>Initializes a new instance of the <see cref="NodeController"/> class.</remarks>
/// <param name="documentExecuter">The document executer.</param>
/// <param name="schema">The GraphQL schema.</param>
/// <param name="serializer">The GraphQL text serializer.</param>
/// <param name="listener">The data loader document listener.</param>
[ApiController]
[Route("api")]
public class NodeController(IDocumentExecuter documentExecuter,
                            ISchema schema,
                            IGraphQLTextSerializer serializer,
                            DataLoaderDocumentListener listener)
    : Controller
{
    /// <summary>Handles GraphQL GET requests.</summary>
    /// <param name="query">The GraphQL query.</param>
    /// <param name="operationName">The operation name.</param>
    /// <returns>The result of the GraphQL query.</returns>
    [HttpGet]
    [ActionName("graphql")]
    public Task<IActionResult> GraphQLGetAsync(string query, string? operationName) =>
        HttpContext.WebSockets.IsWebSocketRequest ?
        Task.FromResult<IActionResult>(BadRequest()) :
        ExecuteGraphQLRequestAsync(BuildRequest(query, operationName));

    /// <summary>Handles GraphQL POST requests.</summary>
    /// <returns>The result of the GraphQL query.</returns>
    [HttpPost("graphql")]
    public async Task<IActionResult> GraphQLAsync()
    {
        if (HttpContext.Request.HasFormContentType)
        {
            var request = BuildRequest(
                await HttpContext.Request.ReadFormAsync(HttpContext.RequestAborted));
            return await ExecuteGraphQLRequestAsync(request);
        }
        else if (HttpContext.Request.HasJsonContentType())
        {
            var request = await serializer.ReadAsync<GraphQLRequest>(HttpContext.Request.Body,
                                                                      HttpContext.RequestAborted);
            return await ExecuteGraphQLRequestAsync(request);
        }
        return BadRequest();
    }

    private GraphQLRequest BuildRequest(IFormCollection form) =>
        BuildRequest(form["query"].ToString(),
                     form["operationName"].ToString(),
                     form["variables"].ToString(),
                     form["extensions"].ToString());

    private GraphQLRequest BuildRequest(string query, string? operationName, string? variables = null,
                                        string? extensions = null) => new()
                                        {
                                            Query = query == string.Empty ? null : query,
                                            OperationName = operationName == string.Empty ? null : operationName,
                                            Variables = serializer.Deserialize<Inputs>(variables == string.Empty ? null : variables),
                                            Extensions = serializer.Deserialize<Inputs>(extensions == string.Empty ? null : extensions),
                                        };

    /// <summary>Executes the GraphQL request.</summary>
    /// <param name="request">The GraphQL request.</param>
    /// <returns>The result of the GraphQL query.</returns>
    public async Task<IActionResult> ExecuteGraphQLRequestAsync(GraphQLRequest? request)
    {
        try
        {
            var result = await documentExecuter.ExecuteAsync(s =>
            {
                s.Schema = schema;
                s.Query = request?.Query;
                s.Variables = request?.Variables;
                s.OperationName = request?.OperationName;
                s.RequestServices = HttpContext.RequestServices;
                s.UserContext = new GraphQLUserContext
                {
                    User = HttpContext.User,
                };
                s.UnhandledExceptionDelegate = WaitHandleCannotBeOpenedException;
                s.CancellationToken = HttpContext.RequestAborted;
                s.Listeners.Add(listener);
            });

            return new ExecutionResultActionResult(result);
        }
        catch
        {
            return BadRequest();
        }
    }

    private static Task WaitHandleCannotBeOpenedException(UnhandledExceptionContext context)
    {
#if DEBUG
        context.ErrorMessage = context.Exception.Message;
#else
        if (context.Exception is GitObjectDbException)
        {
            context.ErrorMessage = context.Exception.Message;
        }
#endif
        return Task.CompletedTask;
    }
}
