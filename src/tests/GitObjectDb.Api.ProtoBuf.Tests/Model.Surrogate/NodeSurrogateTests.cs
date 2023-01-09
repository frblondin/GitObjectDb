using GitObjectDb.Api.ProtoBuf.Model;
using GitObjectDb.Api.ProtoBuf.Model.Surrogates;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using static GitObjectDb.Api.ProtoBuf.Tests.NodeQueryServiceTests;

namespace GitObjectDb.Api.ProtoBuf.Tests.Model.Surrogate;
public class NodeSurrogateTests
{
    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public async Task DeserializeCircularReferences(IConnection connection, string committish, ObjectId id)
    {
        // Arrange
        var reply = await QueryCircularReferences(connection, committish, id);

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
