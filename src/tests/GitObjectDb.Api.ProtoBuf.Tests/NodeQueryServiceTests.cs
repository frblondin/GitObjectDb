using AutoFixture;
using FakeItEasy;
using GitObjectDb.Api.ProtoBuf.Model;
using GitObjectDb.Model;
using GitObjectDb.SystemTextJson;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Api.ProtoBuf.Tests;
public class NodeQueryServiceTests
{
    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public async Task SupportsCyclicReferences(IConnection connection, string committish, ObjectId id)
    {
        // Act
        var reply = (INodeQueryReply)await QueryCircularReferences(connection, committish, id);
        var content = reply.NodeContents!.ToList();

        // Assert
        Assert.That(content, Has.Count.EqualTo(2));
    }

#pragma warning disable NUnit1028 // The non-test method is public
    internal static async Task<NodeQueryReply<NodeWithReference>> QueryCircularReferences(IConnection connection, string committish, ObjectId id)
#pragma warning restore NUnit1028 // The non-test method is public
    {
        var node = CreateCircularReferences(id);
        A.CallTo(() => connection.GetNodes<NodeWithReference>(committish, default, default))
            .Returns(new[] { node }.ToCommitEnumerable(id));
        var sut = new NodeQueryService<NodeWithReference>(connection);

        return await sut.QueryNodesAsync(new(committish), id);
    }

    private static NodeWithReference CreateCircularReferences(ObjectId id)
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

    internal class Customization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject(new ObjectId("8839a59286ccc17b01f280a2999d2fdac621eee6"));
            var repository = CreateRepository(fixture);
            var model = CreateDataModel();
            var connection = CreateConnection(repository, model);
            fixture.Inject(connection);
        }

        private static IRepository CreateRepository(IFixture fixture)
        {
            var tree = A.Fake<Tree>();
            A.CallTo(() => tree.Id)
                .Returns(fixture.Create<ObjectId>());
            var commit = A.Fake<Commit>();
            A.CallTo(() => commit.Tree)
                .Returns(tree);
            return A.Fake<IRepository>(o =>
                o.ConfigureFake(fake =>
                    A.CallTo(() => fake.Lookup(default(string)))
                        .Returns(commit)));
        }

        private static IDataModel CreateDataModel() => new ConventionBaseModelBuilder()
            .RegisterType<NodeWithReference>()
            .Build();

        private static IConnection CreateConnection(IRepository repository, IDataModel model) =>
            A.Fake<IConnection>(o =>
                o.ConfigureFake(fake =>
                {
                    A.CallTo(() => fake.Repository).Returns(repository);
                    A.CallTo(() => fake.Serializer).Returns(new NodeSerializer(model));
                    A.CallTo(() => fake.Model).Returns(model);
                }));
    }
}
