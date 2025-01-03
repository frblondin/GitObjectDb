using GraphQL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace GitObjectDb.Api.GraphQL;

/// <summary>Represents an action result that contains an <see cref="ExecutionResult"/>.</summary>
public class ExecutionResultActionResult(ExecutionResult executionResult) : IActionResult
{
    /// <summary>Gets the execution result.</summary>
    public ExecutionResult ExecutionResult { get; } = executionResult;

    /// <summary>Executes the result operation of the action method asynchronously.</summary>
    /// <param name="context">The context in which the result is executed.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    public async Task ExecuteResultAsync(ActionContext context)
    {
        var writer = context.HttpContext.RequestServices.GetRequiredService<IGraphQLSerializer>();
        var response = context.HttpContext.Response;
        response.ContentType = "application/json";
        response.StatusCode = ExecutionResult.Data == null && ExecutionResult.Errors?.Count > 0 ?
            (int)HttpStatusCode.BadRequest :
            (int)HttpStatusCode.OK;
        await writer.WriteAsync(response.Body, ExecutionResult, context.HttpContext.RequestAborted);
    }
}