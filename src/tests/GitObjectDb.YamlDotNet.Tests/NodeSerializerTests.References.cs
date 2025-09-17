using GitDotNet;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tests.Customization;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;

namespace GitObjectDb.YamlDotNet.Tests;

public partial class NodeSerializerTests
{
    [Test]
    public async Task ReferencesAreSupported()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).Customize(new ReferenceCustomization());
        var sut = fixture.Create<IConnection>();
        var name = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        DataPath path = default;
        var transformations = await sut.UpdateAsync("main", async c =>
        {
            var node1 = await c.CreateOrUpdateAsync(new NodeWithReference { Name = name });
            var node2 = await c.CreateOrUpdateAsync(new NodeWithReference { Reference = node1 });
            path = node2.Path;
        });
        var tip = await transformations.CommitAsync(new("foo", signature, signature));

        // Act
        var result = await sut.LookupAsync<NodeWithReference>(tip, path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference, Is.InstanceOf<NodeWithReference>());
            Assert.That(result.Reference.Name, Is.EqualTo(name));
        });
    }

    [Test]
    public async Task CircularReferencesAreSupported()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).Customize(new ReferenceCustomization());
        var sut = fixture.Create<IConnection>();
        var name = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        DataPath path = default;
        var transformations = await sut.UpdateAsync("main", async c =>
        {
            var node1 = await c.CreateOrUpdateAsync(new NodeWithReference { Name = name });
            var node2 = await c.CreateOrUpdateAsync(new NodeWithReference { Reference = node1 });
            node1 = await c.CreateOrUpdateAsync(node1 with { Reference = node2 });
            path = node2.Path;
        });
        var tip = await transformations.CommitAsync(new("foo", signature, signature));

        // Act
        var result = await sut.LookupAsync<NodeWithReference>(tip, path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference, Is.InstanceOf<NodeWithReference>());
            Assert.That(result.Reference.Name, Is.EqualTo(name));
            Assert.That(result.Reference.Reference, Is.SameAs(result));
        });
    }

    [Test]
    public async Task CircularReferencesAndDeprecationAreSupported()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).Customize(new ReferenceCustomization());
        var sut = fixture.Create<IConnection>();
        var signature = fixture.Create<Signature>();

        DataPath path = default;
        var transformations = await sut.UpdateAsync("main", async c =>
        {
            var node1 = await c.CreateOrUpdateAsync(new NodeWithReference { Id = new("node1") });
            var node2 = await c.CreateOrUpdateAsync(new NodeWithReference { Id = new("node2"), Reference = node1 });
            node1 = await c.CreateOrUpdateAsync((NodeWithReferenceOld)node1 with { Reference = node2 });
            path = node1.Path;
        });
        var tip = await transformations.CommitAsync(new("foo", signature, signature));

        // Act
        var result = await sut.LookupAsync<NodeWithReference>(tip, path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Reference, Is.InstanceOf<NodeWithReference>());
            Assert.That(result.Reference.Reference, Is.SameAs(result));
        });
    }

    [Test]
    public async Task MultipleReferencesAreSupported()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).Customize(new ReferenceCustomization());
        var sut = fixture.Create<IConnection>();
        var name1 = fixture.Create<string>();
        var name2 = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        DataPath path = default;
        var transformations = await sut.UpdateAsync("main", async c =>
        {
            var node1 = await c.CreateOrUpdateAsync(new NodeWithMultipleReferences { Name = name1 });
            var node2 = await c.CreateOrUpdateAsync(new NodeWithMultipleReferences { Name = name2 });
            var node3 = await c.CreateOrUpdateAsync(new NodeWithMultipleReferences
            {
                References = [node1, node2],
            });
            path = node3.Path;
        });
        var tip = await transformations.CommitAsync(new("foo", signature, signature));

        // Act
        var result = await sut.LookupAsync<NodeWithMultipleReferences>(tip, path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.References, Has.Exactly(2).Items);
            Assert.That(result.References[0].Name, Is.EqualTo(name1));
        });
    }

    [Test]
    public async Task CircularMultipleReferencesAndDeprecationAreSupported()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).Customize(new ReferenceCustomization());
        var sut = fixture.Create<IConnection>();
        var signature = fixture.Create<Signature>();

        DataPath path = default;
        var transformations = await sut.UpdateAsync("main", async c =>
        {
            var node1 = await c.CreateOrUpdateAsync(new NodeWithMultipleReferences { Id = new("node1") });
            var node2 = await c.CreateOrUpdateAsync(new NodeWithMultipleReferences { Id = new("node2"), References = new[] { node1 } });
            node1 = await c.CreateOrUpdateAsync((NodeWithMultipleReferencesOld)node1 with { References = new[] { node2 } });
            path = node1.Path;
        });
        var tip = await transformations.CommitAsync(new("foo", signature, signature));

        // Act
        var result = await sut.LookupAsync<NodeWithMultipleReferences>(tip, path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.References, Has.Exactly(1).Items);
            Assert.That(result.References[0].References, Has.Exactly(1).Items.SameAs(result));
        });
    }

    [Test]
    public async Task NonExistingReferenceFails()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).Customize(new ReferenceCustomization());
        var sut = fixture.Create<IConnection>();
        var name = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        DataPath path = default;
        var path2 = new DataPath("a", "a.yml", true);
        var transformations = await sut.UpdateAsync("main", async c =>
        {
            var node2 = await c.CreateOrUpdateAsync(new NodeWithReference
            {
                Reference = new NodeWithReference { Name = "other", Path = path2 },
                Name = "test",
            });
            path = node2.Path;
        });
        var tip = await transformations.CommitAsync(new("foo", signature, signature));

        // Act
        Assert.Throws<GitObjectDbException>(() => sut.GetItemsAsync<TreeItem>(tip).ToEnumerable().ToList());
    }

    [Test]
    public async Task MultipleReferencesIsThreadSafe()
    {
        // Arrange
        var fixture = new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).Customize(new ReferenceCustomization());
        var sut = fixture.Create<IConnection>();
        var name = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        const int NumberOfItems = 1_000;
        var transformations = await sut.UpdateAsync("main", async c =>
        {
            var nodeRef = await c.CreateOrUpdateAsync(new NodeWithReference { Name = "Ref" });
            for (var i = 0; i < NumberOfItems; i++)
            {
                await c.CreateOrUpdateAsync(new NodeWithReference { Name = $"Item {i}", Reference = nodeRef });
            }
        });
        var tip = await transformations.CommitAsync(new("foo", signature, signature));

        // Act
        var actual = sut.GetItemsAsync<TreeItem>(tip).ToEnumerable().ToList();

        // Assert
        Assert.That(actual, Has.Count.EqualTo(NumberOfItems + 1));
    }
}
