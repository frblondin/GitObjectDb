using System;
using GitObjectDb.SystemTextJson;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Transport;
using GraphQL.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Api.GraphQL;

[ApiController]
[Route("api")]
[Authorize]
public class NodeController : Controller
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDocumentExecuter _documentExecuter;
    private readonly ISchema _schema;
    private readonly IGraphQLTextSerializer _serializer;
    private readonly DataLoaderDocumentListener _listener;

    public NodeController(IServiceProvider serviceProvider, IDocumentExecuter documentExecuter, ISchema schema, IGraphQLTextSerializer serializer, DataLoaderDocumentListener listener)
    {
        _serviceProvider = serviceProvider;
        _documentExecuter = documentExecuter;
        _schema = schema;
        _serializer = serializer;
        _listener = listener;
    }

    [HttpGet]
    [ActionName("graphql")]
    public Task<IActionResult> GraphQLGetAsync(string query, string? operationName)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            return Task.FromResult<IActionResult>(BadRequest());
        }
        else
        {
            return ExecuteGraphQLRequestAsync(BuildRequest(query, operationName));
        }
    }

    [HttpPost("graphql")]
    public async Task<IActionResult> GraphQL()
    {
        if (HttpContext.Request.HasFormContentType)
        {
            var request = BuildRequest(
                await HttpContext.Request.ReadFormAsync(HttpContext.RequestAborted));
            return await ExecuteGraphQLRequestAsync(request);
        }
        else if (HttpContext.Request.HasJsonContentType())
        {
            var request = await _serializer.ReadAsync<GraphQLRequest>(HttpContext.Request.Body,
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
        Variables = _serializer.Deserialize<Inputs>(variables == string.Empty ? null : variables),
        Extensions = _serializer.Deserialize<Inputs>(extensions == string.Empty ? null : extensions),
    };

    private async Task<IActionResult> ExecuteGraphQLRequestAsync(GraphQLRequest? request)
    {
        try
        {
            NodeSerializerContext.ServiceProvider = _serviceProvider;
            var result = await _documentExecuter.ExecuteAsync(s =>
            {
                s.Schema = _schema;
                s.Query = request?.Query;
                s.Variables = request?.Variables;
                s.OperationName = request?.OperationName;
                s.RequestServices = HttpContext.RequestServices;
                s.UserContext = new GraphQLUserContext
                {
                    User = HttpContext.User,
                };
                s.User = HttpContext.User;
                s.UnhandledExceptionDelegate = WaitHandleCannotBeOpenedException;
                s.CancellationToken = HttpContext.RequestAborted;
                s.Listeners.Add(_listener);
            });

            return new ExecutionResultActionResult(result);
        }
        catch
        {
            return BadRequest();
        }
        finally
        {
            NodeSerializerContext.ServiceProvider = null!;
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
