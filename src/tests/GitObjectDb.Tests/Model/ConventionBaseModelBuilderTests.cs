using AutoFixture;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Tests.Model;

public class ConventionBaseModelBuilderTests
{
    [Test]
    public void LoadsBasicConfiguration()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultServiceProviderCustomization());
        var sut = fixture.Create<ConventionBaseModelBuilder>();

        // Act
        var model = sut
            .RegisterAssemblyTypes(Assembly.GetExecutingAssembly(), t => t == typeof(SomeNode) || t == typeof(SomeChild))
            .Build();

        // Assert
        Assert.That(model.NodeTypes, Has.Exactly(3).Items);
        Assert.Multiple(() =>
        {
            Assert.That(model.NodeTypes[0].Type, Is.EqualTo(typeof(Node)));
            Assert.That(model.NodeTypes[1].Type, Is.EqualTo(typeof(SomeNode)));
            Assert.That(model.NodeTypes[1].Name, Is.EqualTo(SomeNode.FolderName));
            Assert.That(model.NodeTypes[1].Children, Has.Exactly(1).Items);
            Assert.That(model.NodeTypes[1].Children.Single().Type, Is.EqualTo(typeof(SomeChild)));
        });
    }

    [GitFolder(FolderName = FolderName)]
    [HasChild(typeof(SomeChild))]
    public record SomeNode : Node
    {
        internal const string FolderName = "CrazyFolders";
    }

    public record SomeChild : Node
    {
    }
}
