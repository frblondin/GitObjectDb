using GraphQL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace GitObjectDb.Api.GraphQL;

public class ExecutionResultActionResult : IActionResult
{
    public ExecutionResultActionResult(ExecutionResult executionResult)
    {
        ExecutionResult = executionResult;
    }

    public ExecutionResult ExecutionResult { get; }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var writer = context.HttpContext.RequestServices.GetRequiredService<IGraphQLSerializer>();
        var response = context.HttpContext.Response;
        response.ContentType = "application/json";
        response.StatusCode = ExecutionResult.Data == null && ExecutionResult.Errors?.Any() == true ?
            (int)HttpStatusCode.BadRequest :
            (int)HttpStatusCode.OK;
        await writer.WriteAsync(response.Body, ExecutionResult, context.HttpContext.RequestAborted);
    }
}
