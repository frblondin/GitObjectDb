using GitObjectDb.Api.GraphQL.Assets;
using GitObjectDb.Api.GraphQL.GraphModel;
using GitObjectDb.Tests.Assets.Tools;
using GraphQL;
using GraphQL.Conversion;
using GraphQL.Execution;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Models.Organization;
using Models.Organization.Converters;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
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

    public IServiceProvider ServiceProvider { get; private set; }

    public IDocumentExecuter Executer { get; private set; }

    public IConnection Connection { get; private set; }

    public GitObjectDbSchema Schema { get; private set; }

    [SetUp]
    public void Setup()
    {
        Executer = CreateExecuter();
        ServiceProvider = new ServiceCollection()
            .AddOrganizationModel()
            .AddMemoryCache()
            .AddGitObjectDb(c => c.AddSystemTextJson(o => o.Converters.Add(new TimeZoneInfoConverter())))
            .AddGitObjectDbApi()
            .AddGitObjectDbGraphQL(builder =>
            {
                builder.Schema.RegisterTypeMapping<TimeZoneInfo, TimeZoneInfoGraphType>();
                builder.CacheEntryStrategy = e => e.SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(1));
            })
            .AddGitObjectDbConnection(UniqueId.CreateNew().ToString())
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

        DirectoryUtils.Delete(path, continueOnError: true);
    }

    protected async Task<ExecutionResult> AssertQuerySuccessAsync(
        string query,
        string? expected = null,
        Inputs? variables = null,
        object? root = null,
        IDictionary<string, object?>? userContext = null,
        CancellationToken cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null,
        INameConverter? nameConverter = null)
    {
        var queryResult = expected is not null ? CreateQueryResult(expected) : null;
        return await AssertQueryAsync(query,
                                      queryResult,
                                      variables,
                                      root,
                                      userContext,
                                      cancellationToken,
                                      rules,
                                      null,
                                      nameConverter);
    }

    protected async Task<ExecutionResult> AssertQueryAsync(
        string query,
        object? expectedExecutionResultOrJson,
        Inputs? variables = null,
        object? root = null,
        IDictionary<string, object?>? userContext = null,
        CancellationToken? cancellationToken = default,
        IEnumerable<IValidationRule>? rules = null,
        Func<UnhandledExceptionContext, Task>? unhandledExceptionDelegate = null,
        INameConverter? nameConverter = null)
    {
        var schema = Schema;
        schema.NameConverter = nameConverter ?? CamelCaseNameConverter.Instance;
        var runResult = await Executer.ExecuteAsync(options =>
        {
            options.Schema = schema;
            options.Query = query;
            options.Root = root;
            options.Variables = variables;
            options.UserContext = userContext ?? new Dictionary<string, object?>();
            options.CancellationToken = cancellationToken ?? CancellationToken.None;
            options.ValidationRules = rules;
            options.UnhandledExceptionDelegate = unhandledExceptionDelegate ?? (_ => Task.CompletedTask);
            options.UserContext = new Dictionary<string, object?>();
            options.RequestServices = ServiceProvider;
            options.UnhandledExceptionDelegate = context =>
            {
                context.ErrorMessage = context.Exception.Message;
                return Task.CompletedTask;
            };
        }).ConfigureAwait(false);

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

    private static ExecutionResult CreateQueryResult(string result,
                                                     ExecutionErrors? errors = null,
                                                     bool executed = true) =>
        result.ToExecutionResult(errors, executed);
}
