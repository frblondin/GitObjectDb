using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using FakeItEasy;
using GitDotNet;
using GitObjectDb.Api.ProtoBuf.Model;
using GitObjectDb.Model;
using GitObjectDb.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace GitObjectDb.Api.ProtoBuf.Tests;
public class NodeQueryServiceTests
{
    [Test]
    public async Task SupportsCyclicReferences()
    {
        // Arrange
        var fixture = await new Fixture().CustomizeAsync<Customization>();
        var connection = fixture.Create<IConnection>();
        var commit = fixture.Create<CommitEntry>();
        var id = fixture.Create<HashId>();

        // Act
        var reply = (INodeQueryReply)await QueryCircularReferences(connection, commit, id);
        var content = reply.NodeContents!.ToList();

        // Assert
        Assert.That(content, Has.Count.EqualTo(2));
    }

#pragma warning disable NUnit1028 // The non-test method is public
    internal static async Task<NodeQueryReply<NodeWithReference>> QueryCircularReferences(IConnection connection, CommitEntry commit, HashId id)
#pragma warning restore NUnit1028 // The non-test method is public
    {
        var node = CreateCircularReferences(id);
        A.CallTo(() => connection.GetNodesAsync<NodeWithReference>(commit, default, default, A<CancellationToken>._))
            .Returns(new[] { node }.ToAsyncEnumerable());
        var sut = new NodeQueryService<NodeWithReference>(connection);

        return await sut.QueryNodesAsync(new(commit.Id.ToString()), id);
    }

    private static NodeWithReference CreateCircularReferences(HashId id)
    {
        var result = new NodeWithReference
        {
            Path = DataPath.Parse("NodeWithReferences/nodeA.json"),
            TreeId = id,
        };
        var reference = new NodeWithReference
        {
            Path = DataPath.Parse("NodeWithReferences/nodeB.json"),
            TreeId = id,
            Reference = result,
        };
        result.Reference = reference;
        return result;
    }

    public record NodeWithReference : Node
    {
        public NodeWithReference? Reference { get; set; }
    }

    internal class Customization : IAsyncCustomization
    {
        public async Task CustomizeAsync(IFixture fixture)
        {
            var repository = await CreateRepositoryAsync(fixture);
            var model = CreateDataModel();
            var connection = CreateConnection(repository, model);
            fixture.Inject(connection);
        }

        private static async Task<IGitConnection> CreateRepositoryAsync(IFixture fixture)
        {
            var connection = new ServiceCollection()
                .AddMemoryCache()
                .AddGitDotNet()
                .BuildServiceProvider()
                .GetRequiredService<GitConnectionProvider>()
                .Invoke(".");
            fixture.Inject(connection);
            var commit = await connection.GetCommittishAsync("HEAD");
            fixture.Inject(commit);
            fixture.Inject(commit.Id);
            var tree = await commit.GetRootTreeAsync();
            fixture.Inject(tree);
            return connection;
        }

        private static IDataModel CreateDataModel() => new ConventionBaseModelBuilder()
            .RegisterType<NodeWithReference>()
            .Build();

        private static IConnection CreateConnection(IGitConnection repository, IDataModel model) =>
            A.Fake<IConnection>(o =>
                o.ConfigureFake(fake =>
                {
                    A.CallTo(() => fake.Repository).Returns(repository);
                    A.CallTo(() => fake.Serializer).Returns(new NodeSerializer(model));
                    A.CallTo(() => fake.Model).Returns(model);
                }));
    }
}
