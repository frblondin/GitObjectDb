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
    protected Func<Task<Response<object>>> CreateGraphQLStep(string query, IDocumentExecuter executer) => async () =>
    {
        var result = await executer.ExecuteAsync(options =>
        {
            options.Schema = Schema;
            options.Query = query;
            options.UserContext = new Dictionary<string, object?>();
            options.RequestServices = ServiceProvider;
        }).ConfigureAwait(false);
        return result.Errors?.Any() ?? true ?
                Response.Ok() :
                Response.Fail(message: result.Errors!.First().Message);
    };

    protected static void RunScenarios(params ScenarioProps[] scenarios)
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
