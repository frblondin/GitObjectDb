using GitObjectDb.Api.ProtoBuf.Model;
using GitObjectDb.Api.ProtoBuf.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using Grpc.Net.Client;
using Models.Organization;
using NUnit.Framework;
using ProtoBuf.Grpc.Client;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Api.ProtoBuf.Tests;

public class QueryTests
{
    [Test]
    [AutoDataCustomizations(typeof(TestServerCustomization))]
    public async Task ReadData(GrpcChannel channel, IQueryAccessor queryAccessor)
    {
        // Act
        var client = channel.CreateGrpcService<INodeQueryService<Organization>>();
        var result = await client.QueryNodesAsync(new("main", IsRecursive: true));

        // Assert
        var nodes = queryAccessor.GetNodes<Organization>("main", isRecursive: true).ToList();
        Assert.That(result.Nodes, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(result.Nodes.Count(), Is.EqualTo(nodes.Count));
            Assert.That(result.Nodes.First().Id, Is.EqualTo(nodes[0].Id));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(TestServerCustomization))]
    public async Task ReadDeltaData(GrpcChannel channel, IConnection connection, string newLabel, CommitDescription commitDescription)
    {
        // Arrange
        var node = connection.GetNodes<Organization>("main").First();
        var commit = connection
            .Update("main", c => c.CreateOrUpdate(node with { Label = newLabel }))
            .Commit(commitDescription);

        // Act
        var client = channel.CreateGrpcService<INodeQueryService<Organization>>();
        var result = await client.QueryNodeDeltasAsync(new("main~1", "main"));

        // Assert
        var changes = connection.Compare("main~1", "main");
        Assert.That(result.Changes, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(result.Changes.Count(), Is.EqualTo(changes.Count));
            Assert.That(result.Changes.First().Old!.TreeId, Is.EqualTo(node.TreeId));
            Assert.That(result.Changes.First().New!.TreeId, Is.EqualTo(commit.Tree.Id));
            Assert.That(result.Changes.First().New!.Label, Is.EqualTo(newLabel));
        });
    }
}
