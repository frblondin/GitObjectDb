using GitObjectDb.Api.GraphQL.Tests.Assets;
using GraphQL;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Network.Ping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Api.GraphQL.Tests;

public class LoadTestBase : QueryTestBase
{
    private readonly IClientFactory<IDocumentExecuter> _clientFactory =
        ClientFactory.Create("graphql_executer_factory", (_, _) => Task.FromResult(CreateExecuter()));

    protected IStep CreateGraphQLStep(string name, string query, TimeSpan? timeout = null) => Step.Create(
        name,
        clientFactory: _clientFactory,
        execute: async context =>
        {
            var result = await context.Client.ExecuteAsync(options =>
            {
                options.Schema = Schema;
                options.Query = query;
                options.CancellationToken = context.CancellationToken;
                options.UserContext = new Dictionary<string, object?>();
                options.RequestServices = ServiceProvider;
            }).ConfigureAwait(false);
            return result.Errors?.Any() ?? true ?
                   Response.Ok() :
                   Response.Fail(result.Errors!.First().Message);
        },
        timeout);

    protected static void RunScenarios(params Scenario[] scenarios)
    {
        // creates ping plugin that brings additional reporting data
        var pingPluginConfig = PingPluginConfig.CreateDefault(new[] { "nbomber.com" });
        var pingPlugin = new PingPlugin(pingPluginConfig);

        NBomberRunner
            .RegisterScenarios(scenarios)
            .WithWorkerPlugins(pingPlugin)
            .Run();
    }
}
