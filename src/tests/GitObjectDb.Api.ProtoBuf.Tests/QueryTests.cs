using AutoFixture;
using GitObjectDb.Api.ProtoBuf.Model;
using GitObjectDb.Api.ProtoBuf.Tests.Assets;
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
    public async Task ReadData()
    {
        // Arrange
        var fixture = new Fixture().Customize(new TestServerCustomization());
        var channel = fixture.Create<GrpcChannel>();
        using var connection = fixture.Create<IConnection>();

        // Act
        var client = channel.CreateGrpcService<INodeQueryService<Organization>>();
        var result = await client.QueryNodesAsync(new("main", IsRecursive: true));

        // Assert
        var tip = await connection.Repository.GetCommittishAsync("main");
        var nodes = connection.GetNodesAsync<Organization>(tip, isRecursive: true).ToEnumerable().ToList();
        Assert.That(result.Nodes, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(result.Nodes.Count(), Is.EqualTo(nodes.Count));
            Assert.That(result.Nodes.First().Id, Is.EqualTo(nodes[0].Id));
        });
    }

    [Test]
    public async Task ReadDeltaData()
    {
        // Arrange
        var fixture = new Fixture().Customize(new TestServerCustomization());
        var channel = fixture.Create<GrpcChannel>();
        using var connection = fixture.Create<IConnection>();
        var newLabel = fixture.Create<string>();
        var tip = await connection.Repository.GetCommittishAsync("main");
        var node = connection.GetNodesAsync<Organization>(tip).ToEnumerable().First();
        var changes = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(node with { Label = newLabel }));
        var commit = await changes.CommitAsync(fixture.Create<CommitDescription>());

        // Act
        var client = channel.CreateGrpcService<INodeQueryService<Organization>>();
        var result = await client.QueryNodeDeltasAsync(new("main~1", "main"));

        // Assert
        var diff = await connection.CompareAsync("main~1", "main");
        var tree = await commit.GetRootTreeAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Changes, Is.Not.Empty);
            Assert.That(result.Changes!.Count(), Is.EqualTo(diff.Count));
            var change = result.Changes!.First();
            Assert.That(change.Old!.TreeId, Is.EqualTo(node.TreeId));
            Assert.That(change.New!.TreeId, Is.EqualTo(tree.Id));
            Assert.That(change.New!.Label, Is.EqualTo(newLabel));
        });
    }
}
