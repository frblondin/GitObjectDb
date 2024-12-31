using GitObjectDb.Api.GraphQL.Assets;
using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Tests.Assets.Tools;
using GraphQL;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Models.Organization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitObjectDb.Api.GraphQL.Tests.Assets;

#pragma warning disable SA1402 // File may only contain a single type
public class QueryTestBase : QueryTestBase<GraphQLDocumentBuilder>
{
}

public class QueryTestBase<TDocumentBuilder>
    where TDocumentBuilder : IDocumentBuilder, new()
{
    internal const string Commit = "$commit";

    public IGraphQLTextSerializer Serializer { get; private set; } = new GraphQLSerializer(new JsonSerializerOptions()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // less strict about what is encoded into \uXXXX
    });

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public ServiceProvider ServiceProvider { get; private set; }

    public IDocumentExecuter Executer { get; private set; }

    public IConnection Connection { get; private set; }

    public GitObjectDbSchema Schema { get; private set; }
#pragma warning restore CS8618

    [SetUp]
    public void Setup()
    {
        Executer = CreateExecuter();
        ServiceProvider = new ServiceCollection()
            .AddOrganizationModel()
            .AddMemoryCache()
            .AddGitObjectDb()
            .AddGitObjectDbSystemTextJson(o => o.ConfigureForNodaTime(Organization.TimeZoneProvider))
            .AddGitObjectDbGraphQLSchema(o =>
            {
                o.ConfigureSchema = s => s.RegisterTypeMapping<DateTimeZone, DateTimeZoneGraphType>();
                o.CacheEntryStrategy = e => e.SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(1));
            })
            .AddGraphQL(builder => builder.AddSystemTextJson())
            .AddGitObjectDbConnection(UniqueId.CreateNew().ToString())
            .AddTransient<NodeController>()
            .BuildServiceProvider();
        Schema = (GitObjectDbSchema)ServiceProvider.GetRequiredService<ISchema>();
        Connection = ServiceProvider.GetRequiredService<IConnection>();
    }

    protected static IDocumentExecuter CreateExecuter() =>
        new DocumentExecuter(new TDocumentBuilder(), new DocumentValidator());

    [OneTimeSetUp]
    public void CleanUpPastExecutions()
    {
        DirectoryUtils.Delete(ConnectionProvider.ReposPath, continueOnError: true);
    }

    [TearDown]
    public void CleanUp()
    {
        var path = Connection.Repository.Info.Path;
        Connection.Dispose();
        ServiceProvider.Dispose();

        DirectoryUtils.Delete(path, continueOnError: true);
    }

    protected async Task<ExecutionResult> AssertQuerySuccessAsync(
        string query,
        string? expected = null,
        Inputs? variables = null)
    {
        var queryResult = expected is not null ? CreateQueryResult(expected) : null;
        return await AssertQueryAsync(query,
                                      queryResult,
                                      variables);
    }

    protected async Task<ExecutionResult> AssertQueryAsync(
        string query,
        object? expectedExecutionResultOrJson,
        Inputs? variables = null)
    {
        var controller = ServiceProvider.GetRequiredService<NodeController>();
        controller.ControllerContext = new()
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = ServiceProvider,
            },
        };
        controller.ControllerContext.HttpContext.Request.Form = new FormCollection(new()
        {
            ["query"] = query,
            ["variables"] = Serializer.Serialize(variables),
        });
        var request = await controller.GraphQLAsync().ConfigureAwait(false);
        var runResult = ((ExecutionResultActionResult)request).ExecutionResult;
        AssertQuerySuccess(runResult);

        if (expectedExecutionResultOrJson is not null)
        {
            var writtenResult = Serializer.Serialize(runResult);
            var additionalInfo = string.Empty;

            if (runResult.Errors?.Any() == true)
            {
                additionalInfo += string.Join(Environment.NewLine, runResult.Errors
                    .Where(x => x.InnerException is GraphQLSyntaxErrorException)
                    .Select(x => x.InnerException!.Message));
            }

            var expectedResult = expectedExecutionResultOrJson is string s ?
                s :
                Serializer.Serialize((ExecutionResult)expectedExecutionResultOrJson);
            Assert.That(writtenResult,
                        Does.Match(expectedResult.Replace(Commit, @"\w+")),
                        () => additionalInfo);
        }

        return runResult;
    }

    protected void AssertQuerySuccess(ExecutionResult result)
    {
        if (result.Errors?.Any() ?? false)
        {
            throw new ExecutionError(string.Join('\n', result.Errors!.Select(e => e.Message)));
        }
    }

    private static ExecutionResult CreateQueryResult(string result,
                                                     ExecutionErrors? errors = null,
                                                     bool executed = true) =>
        result.ToExecutionResult(errors, executed);
}
