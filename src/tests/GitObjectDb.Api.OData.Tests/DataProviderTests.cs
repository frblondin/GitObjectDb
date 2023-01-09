using AutoFixture;
using AutoFixture.Kernel;
using AutoMapper;
using FakeItEasy;
using Fasterflect;
using GitObjectDb.Api.OData.Model;
using GitObjectDb.Api.OData.Tests.Model;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static GitObjectDb.Api.OData.Tests.Model.BasicModel;

namespace GitObjectDb.Api.OData.Tests;

public class DataProviderTests
{
    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void QuerySimpleNodes(SimpleNode[] nodes)
    {
        // Arrange
        var sut = CreateDataProvider(nodes, out var typeEmitter);
        var simpleNodeDto =
            typeEmitter.TypeDescriptions.Single(d => d.NodeType.Type == typeof(SimpleNode));

        // Act
        var result = ((IEnumerable<NodeDto>)typeof(DataProvider).GetMethod(nameof(DataProvider.GetNodes))!
                .MakeGenericMethod(typeof(SimpleNode), simpleNodeDto.DtoType)
                .Invoke(sut, new object[] { null, null, false }))!
            .ToList();

        // Arrange
        Assert.That(result, Has.Exactly(nodes.Length).Items);
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Node, Is.SameAs(nodes[0]));
            Assert.That(result[0].Id, Is.EqualTo(nodes[0].Id.ToString()));
            Assert.That(result[0].Path, Is.EqualTo(nodes[0].Path!.FilePath));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void QueryNodesAndReferences(MultiReferenceNode[] nodes, SimpleNode node)
    {
        // Arrange
        var sut = CreateDataProvider(nodes, out var typeEmitter);
        var multiReferenceNodeDto =
            typeEmitter.TypeDescriptions.Single(d => d.NodeType.Type == typeof(MultiReferenceNode));

        // Act
        var result = ((IEnumerable<NodeDto>)typeof(DataProvider).GetMethod(nameof(DataProvider.GetNodes))!
                .MakeGenericMethod(typeof(MultiReferenceNode), multiReferenceNodeDto.DtoType)
                .Invoke(sut, new object[] { null, null, false }))!
            .ToList();

        // Arrange
        Assert.That(result, Has.Exactly(nodes.Length).Items);
        var firstReference = ((IEnumerable<NodeDto>)Reflect
                .Getter(multiReferenceNodeDto.DtoType, nameof(MultiReferenceNode.MultiReference))
                .Invoke(result[0]))
            .First();
        Assert.Multiple(() =>
        {
            Assert.That(firstReference.Node, Is.SameAs(nodes[0].MultiReference[0]));
            Assert.That(firstReference.Id, Is.EqualTo(nodes[0].MultiReference[0].Id.ToString()));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void QueryNodeChildren(SimpleNode node, MultiReferenceNode[] children)
    {
        // Arrange
        var sut = CreateDataProvider(Enumerable.Repeat(node, 1), out var typeEmitter);
        var simpleNodeDto =
            typeEmitter.TypeDescriptions.Single(d => d.NodeType.Type == typeof(SimpleNode));
        var accessor = sut.QueryAccessor;
        A.CallTo(() => accessor.GetNodes<Node>(ObjectId.Zero.Sha.ToString(), node, false))
            .Returns(children.ToCommitEnumerable(ObjectId.Zero));

        // Act
        var result = ((IEnumerable<NodeDto>)typeof(DataProvider).GetMethod(nameof(DataProvider.GetNodes))!
                .MakeGenericMethod(typeof(SimpleNode), simpleNodeDto.DtoType)
                .Invoke(sut, new object[] { null, null, false }))!
            .ToList();
        var resolvedChildren = result[0].Children.ToList();

        // Arrange
        Assert.That(resolvedChildren, Has.Exactly(children.Length).Items);
        Assert.Multiple(() =>
        {
            Assert.That(resolvedChildren[0].Node, Is.SameAs(children[0]));
            Assert.That(resolvedChildren[0].Id, Is.EqualTo(children[0].Id.ToString()));
        });
    }

    private static DataProvider CreateDataProvider<TNode>(IEnumerable<TNode> nodes, out DtoTypeEmitter typeEmitter)
        where TNode : Node
    {
        var emitter = typeEmitter = new DtoTypeEmitter(CreateDataModel(typeof(BasicModel).GetNestedTypes()));
        var mapper = new Mapper(
            new MapperConfiguration(
                c => c.AddProfile(new AutoMapperProfile(emitter.TypeDescriptions))));
        var queryAccessor = A.Fake<IQueryAccessor>();
        A.CallTo(() => queryAccessor.GetNodes<TNode>(default, default, default))
            .Returns(nodes.ToCommitEnumerable(ObjectId.Zero));
        return new DataProvider(queryAccessor, mapper, new MemoryCache(Options.Create(new MemoryCacheOptions())));
    }

    private class Customization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register<ResourceLink>(() => null);
            fixture.Register<ObjectId>(() => null);
        }
    }
}