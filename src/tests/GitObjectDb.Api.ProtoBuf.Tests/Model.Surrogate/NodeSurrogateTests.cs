using AutoFixture;
using GitDotNet;
using GitObjectDb.Api.ProtoBuf.Model;
using GitObjectDb.Api.ProtoBuf.Model.Surrogates;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using static GitObjectDb.Api.ProtoBuf.Tests.NodeQueryServiceTests;

namespace GitObjectDb.Api.ProtoBuf.Tests.Model.Surrogate;
public class NodeSurrogateTests
{
    [Test]
    public async Task DeserializeCircularReferences()
    {
        // Arrange
        var fixture = await new Fixture().CustomizeAsync<Customization>();
        var connection = fixture.Create<IConnection>();
        var commit = fixture.Create<CommitEntry>();
        var id = fixture.Create<HashId>();

        var reply = await QueryCircularReferences(connection, commit, id);

        // Act
        NodeQueryReply.Current = reply;
        IServiceProviderExtensions.Serializer = connection.Serializer;
        try
        {
            var surrogate = (NodeSurrogate<NodeWithReference>)reply.Nodes!.First()!;
            var deserialized = (NodeWithReference)surrogate!;
            Assert.Multiple(() =>
            {
                Assert.That(deserialized, Is.Not.Null);
                Assert.That(deserialized!.Reference, Is.Not.Null);
                Assert.That(deserialized.Reference!.Reference, Is.SameAs(deserialized));
            });
        }
        finally
        {
            NodeQueryReply.ResetCurrent();
            IServiceProviderExtensions.Serializer = null;
        }
    }
}
