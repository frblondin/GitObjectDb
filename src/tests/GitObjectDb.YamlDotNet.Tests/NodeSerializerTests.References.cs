using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tests.Customization;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitObjectDb.YamlDotNet.Tests;

public partial class NodeSerializerTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization), typeof(ReferenceCustomization))]
    public void ReferencesAreSupported(IConnection sut, string name, Signature signature)
    {
        // Arrange
        DataPath path = default;
        sut.Update("main", c =>
        {
            var node1 = c.CreateOrUpdate(new NodeWithReference { Name = name });
            var node2 = c.CreateOrUpdate(new NodeWithReference { Reference = node1 });
            path = node2.Path;
        }).Commit(new("foo", signature, signature));

        // Act
        var result = sut.Lookup<NodeWithReference>("main", path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference, Is.InstanceOf<NodeWithReference>());
            Assert.That(result.Reference.Name, Is.EqualTo(name));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization), typeof(ReferenceCustomization))]
    public void CircularReferencesAreSupported(IConnection sut, string name, Signature signature)
    {
        // Arrange
        DataPath path = default;
        sut.Update("main", c =>
        {
            var node1 = c.CreateOrUpdate(new NodeWithReference { Name = name });
            var node2 = c.CreateOrUpdate(new NodeWithReference { Reference = node1 });
            node1 = c.CreateOrUpdate(node1 with { Reference = node2 });
            path = node2.Path;
        }).Commit(new("foo", signature, signature));

        // Act
        var result = sut.Lookup<NodeWithReference>("main", path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference, Is.InstanceOf<NodeWithReference>());
            Assert.That(result.Reference.Name, Is.EqualTo(name));
            Assert.That(result.Reference.Reference, Is.SameAs(result));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization), typeof(ReferenceCustomization))]
    public void CircularReferencesAndDeprecationAreSupported(IConnection sut, Signature signature)
    {
        // Arrange
        DataPath path = default;
        sut.Update("main", c =>
        {
            var node1 = c.CreateOrUpdate(new NodeWithReference { Id = new("node1") });
            var node2 = c.CreateOrUpdate(new NodeWithReference { Id = new("node2"), Reference = node1 });
            node1 = c.CreateOrUpdate((NodeWithReferenceOld)node1 with { Reference = node2 });
            path = node1.Path;
        }).Commit(new("foo", signature, signature));

        // Act
        var result = sut.Lookup<NodeWithReference>("main", path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference, Is.InstanceOf<NodeWithReference>());
            Assert.That(result.Reference.Reference, Is.SameAs(result));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization), typeof(ReferenceCustomization))]
    public void MultipleReferencesAreSupported(IConnection sut, string name1, string name2, Signature signature)
    {
        // Arrange
        DataPath path = default;
        sut.Update("main", c =>
        {
            var node1 = c.CreateOrUpdate(new NodeWithMultipleReferences { Name = name1 });
            var node2 = c.CreateOrUpdate(new NodeWithMultipleReferences { Name = name2 });
            var node3 = c.CreateOrUpdate(new NodeWithMultipleReferences
            {
                References = new[] { node1, node2 },
            });
            path = node3.Path;
        }).Commit(new("foo", signature, signature));

        // Act
        var result = sut.Lookup<NodeWithMultipleReferences>("main", path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.References, Has.Exactly(2).Items);
            Assert.That(result.References[0].Name, Is.EqualTo(name1));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization), typeof(ReferenceCustomization))]
    public void CircularMultipleReferencesAndDeprecationAreSupported(IConnection sut, Signature signature)
    {
        // Arrange
        DataPath path = default;
        sut.Update("main", c =>
        {
            var node1 = c.CreateOrUpdate(new NodeWithMultipleReferences { Id = new("node1") });
            var node2 = c.CreateOrUpdate(new NodeWithMultipleReferences { Id = new("node2"), References = new[] { node1 } });
            node1 = c.CreateOrUpdate((NodeWithMultipleReferencesOld)node1 with { References = new[] { node2 } });
            path = node1.Path;
        }).Commit(new("foo", signature, signature));

        // Act
        var result = sut.Lookup<NodeWithMultipleReferences>("main", path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.References, Has.Exactly(1).Items);
            Assert.That(result.References[0].References, Has.Exactly(1).Items.SameAs(result));
        });
    }
}
