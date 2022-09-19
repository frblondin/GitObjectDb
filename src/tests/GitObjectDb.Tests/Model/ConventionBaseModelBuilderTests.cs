using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Tests.Model;

public class ConventionBaseModelBuilderTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
    public void LoadsBasicConfiguration(ConventionBaseModelBuilder sut)
    {
        var model = sut
            .RegisterAssemblyTypes(Assembly.GetExecutingAssembly(), t => t == typeof(SomeNode) || t == typeof(SomeChild))
            .Build();

        Assert.That(model.NodeTypes, Has.Exactly(2).Items);
        Assert.Multiple(() =>
        {
            Assert.That(model.NodeTypes.First().Type, Is.EqualTo(typeof(SomeNode)));
            Assert.That(model.NodeTypes.First().Name, Is.EqualTo(SomeNode.FolderName));
            Assert.That(model.NodeTypes.First().Children, Has.Exactly(1).Items);
            Assert.That(model.NodeTypes.First().Children.Single().Type, Is.EqualTo(typeof(SomeChild)));
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
