using GitObjectDb.Api.GraphQL.Tests.Assets;
using GraphQL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;

namespace GitObjectDb.Api.GraphQL.Tests;
internal class ExecutionResultActionResultTests : QueryTestBase
{
    [Test]
    public async Task SuccessfulResultReturnsOkStatusCode()
    {
        // Arrange
        var context = CreateContext();
        var response = context.HttpContext.Response;

        // Act
        await new ExecutionResultActionResult(new()
        {
            Data = new object(),
        }).ExecuteResultAsync(context);

        // Arrange
        Assert.Multiple(() =>
        {
            Assert.That(response.ContentType, Is.EqualTo("application/json"));
            Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task FailingResultReturnsBadStatusCode()
    {
        // Arrange
        var context = CreateContext();
        var response = context.HttpContext.Response;

        // Act
        await new ExecutionResultActionResult(new()
        {
            Errors = new()
            {
                new ExecutionError("error"),
            },
        }).ExecuteResultAsync(context);

        // Arrange
        Assert.Multiple(() =>
        {
            Assert.That(response.ContentType, Is.EqualTo("application/json"));
            Assert.That(response.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
        });
    }

    private ActionContext CreateContext() => new()
    {
        HttpContext = new DefaultHttpContext
        {
            RequestServices = ServiceProvider,
        },
    };
}
