using Bogus;
using GraphQL;
using LibGit2Sharp;
using Models.Organization;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Plugins.Network.Ping;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IgnoreAttribute = NUnit.Framework.IgnoreAttribute;

namespace GitObjectDb.Api.GraphQL.Tests;

public class LoadTests : LoadTestBase
{
    private const int TotalNodeCount = 100_000;
    private const int CommitCount = 5;
    private const int UpdateNodeCountPerCommit = 20;

    [Ignore("Scenario to be refined and assertions to be added.")]
    [Test]
    public void ReadConcurrentAccesses()
    {
        var executer = CreateExecuter();
        var scenario = Scenario
            .Create(nameof(ReadConcurrentAccesses), async context =>
            {
                await Step.Run("GetDelta", context, CreateGraphQLStep(@"
                {
                  organizationsDelta(start: ""HEAD~4"") {
                    updatedAt
                    deleted
                    old {
                      graphicalOrganizationStructureId
                    }
                    new {
                      graphicalOrganizationStructureId
                    }
                  }
                }", executer));
                await Step.Run("Pause", context, async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    return Response.Ok();
                });
                return Response.Ok();
            })
            .WithInit(InitializeRepository)
            .WithLoadSimulations(
                Simulation.RampingConstant(copies: 10_000, during: TimeSpan.FromSeconds(10)),
                Simulation.KeepConstant(copies: 10_000, during: TimeSpan.FromSeconds(20)));

        RunScenarios(scenario);
    }

    private Task InitializeRepository(IScenarioInitContext context)
    {
        var generator = new DataGenerator(Connection, TotalNodeCount, 5);
        generator.CreateInitData();

        for (int i = 0; i < CommitCount; i++)
        {
            generator.UpdateRandomNodes(
                UpdateNodeCountPerCommit,
                n => n with { GraphicalOrganizationStructureId = UniqueId.CreateNew().ToString() },
                $"New round of changes #{i}");
        }

        return Task.CompletedTask;
    }
}
