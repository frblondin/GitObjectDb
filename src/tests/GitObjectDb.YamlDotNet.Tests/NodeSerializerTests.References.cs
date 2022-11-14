using GitObjectDb.Model;
using GitObjectDb.Tests;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace GitObjectDb.YamlDotNet.Tests;

public partial class NodeSerializerTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization))]
    public void ReferencesAreSupported(IServiceProvider serviceProvider, string name, Signature signature)
    {
        // Arrange
        var sut = CreateRepository(serviceProvider);
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
            Assert.That(((NodeWithReference)result.Reference).Name, Is.EqualTo(name));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization))]
    public void CircularReferencesAreSupported(IServiceProvider serviceProvider, string name, Signature signature)
    {
        // Arrange
        var sut = CreateRepository(serviceProvider);
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
            Assert.That(((NodeWithReference)result.Reference).Name, Is.EqualTo(name));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultYamlServiceProviderCustomization))]
    public void MultipleReferencesAreSupported(IServiceProvider serviceProvider, string name1, string name2, Signature signature)
    {
        // Arrange
        var sut = CreateRepository(serviceProvider);
        DataPath path = default;
        sut.Update("main", c =>
        {
            var node1 = c.CreateOrUpdate(new NodeWithMultipleReference { Name = name1 });
            var node2 = c.CreateOrUpdate(new NodeWithMultipleReference { Name = name2 });
            var node3 = c.CreateOrUpdate(new NodeWithMultipleReference
            {
                References = new[] { node1, node2 },
            });
            path = node3.Path;
        }).Commit(new("foo", signature, signature));

        // Act
        var result = sut.Lookup<NodeWithMultipleReference>("main", path);

        // Act, Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.References, Has.Exactly(2).Items);
            Assert.That(((NodeWithMultipleReference)result.References[0]).Name, Is.EqualTo(name1));
        });
    }

    private static IConnection CreateRepository(IServiceProvider serviceProvider)
    {
        var path = GitObjectDbYamlDotNetFixture.GetAvailableFolderPath();
        var repositoryFactory = serviceProvider.GetRequiredService<ConnectionFactory>();
        var model = new ConventionBaseModelBuilder()
            .RegisterType<NodeWithReference>()
            .RegisterType<NodeWithMultipleReference>()
            .Build();
        var result = repositoryFactory(path, model);
        return result;
    }

    public record NodeWithReference : Node
    {
        public string Name { get; set; }

        public Node Reference { get; set; }
    }

    public record NodeWithMultipleReference : Node
    {
        public string Name { get; set; }

        public IList<Node> References { get; set; }
    }
}
