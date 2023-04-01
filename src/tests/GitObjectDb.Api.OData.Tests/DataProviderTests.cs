using AutoFixture;
using AutoMapper;
using FakeItEasy;
using Fasterflect;
using GitObjectDb.Api.OData.Model;
using GitObjectDb.Api.OData.Tests.Model;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using static GitObjectDb.Api.OData.Tests.Model.BasicModel;

namespace GitObjectDb.Api.OData.Tests;

public class DataProviderTests
{
    [Test]
    [AutoDataCustomizations(typeof(Customization))]
    public void QuerySimpleNodes(SimpleNode[] nodes,
                                 IQueryAccessor queryAccessor,
                                 IApplicationPartTypeProvider typeProvider,
                                 DataProvider dataProvider)
    {
        // Arrange
        A.CallTo(() => queryAccessor.GetNodes<SimpleNode>(default, default, default))
            .Returns(nodes.ToCommitEnumerable(ObjectId.Zero));
        var simpleNodeDto = GetDtoDescription<SimpleNode>(typeProvider, dataProvider);

        // Act
        var result = dataProvider.GetNodes<SimpleNode>(simpleNodeDto, default).ToList();

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
    public void QueryNodesAndReferences(MultiReferenceNode[] nodes,
                                        SimpleNode node,
                                        IQueryAccessor queryAccessor,
                                        IApplicationPartTypeProvider typeProvider,
                                        DataProvider dataProvider)
    {
        // Arrange
        A.CallTo(() => queryAccessor.GetNodes<MultiReferenceNode>(default, default, default))
            .Returns(nodes.ToCommitEnumerable(ObjectId.Zero));
        var multiReferenceNodeDto = GetDtoDescription<MultiReferenceNode>(typeProvider, dataProvider);

        // Act
        var result = dataProvider.GetNodes<MultiReferenceNode>(multiReferenceNodeDto, default).ToList();

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
    public void QueryNodeChildren(SimpleNode node,
                                  MultiReferenceNode[] children,
                                  IQueryAccessor queryAccessor,
                                  IApplicationPartTypeProvider typeProvider,
                                  DataProvider dataProvider)
    {
        // Arrange
        A.CallTo(() => queryAccessor.GetNodes<SimpleNode>(default, default, default))
            .Returns(Enumerable.Repeat(node, 1).ToCommitEnumerable(ObjectId.Zero));
        A.CallTo(() => queryAccessor.GetNodes<Node>(ObjectId.Zero.Sha.ToString(), node, false))
            .Returns(children.ToCommitEnumerable(ObjectId.Zero));
        var simpleNodeDto = GetDtoDescription<SimpleNode>(typeProvider, dataProvider);

        // Act
        var result = dataProvider.GetNodes<SimpleNode>(simpleNodeDto, default).ToList();
        var resolvedChildren = result[0].Children.ToList();

        // Arrange
        Assert.That(resolvedChildren, Has.Exactly(children.Length).Items);
        Assert.Multiple(() =>
        {
            Assert.That(resolvedChildren[0].Node, Is.SameAs(children[0]));
            Assert.That(resolvedChildren[0].Id, Is.EqualTo(children[0].Id.ToString()));
        });
    }

    private static DataTransferTypeDescription GetDtoDescription<TNode>(IApplicationPartTypeProvider typeProvider, DataProvider dataProvider)
        where TNode : Node
    {
        var controller = typeProvider.Types.Single(t => t.BaseType.GetGenericArguments()[0] == typeof(TNode));
        var emitter = ((GeneratedTypesApplicationPart)typeProvider).Emitter;
        var instance = Reflect.Constructor(controller, typeof(DataProvider), typeof(DtoTypeEmitter)).Invoke(dataProvider, emitter);
        return emitter.TypeDescriptions.Single(d => d.NodeType.Type == typeof(TNode));
    }

    private class Customization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register<ResourceLink>(() => null);
            fixture.Register<ObjectId>(() => null);

            var emitter = new DtoTypeEmitter(CreateDataModel(typeof(BasicModel).GetNestedTypes()));
            var part = new GeneratedTypesApplicationPart(emitter);
            fixture.Inject<IApplicationPartTypeProvider>(part);
            var mapper = new Mapper(
                new MapperConfiguration(
                    c => c.AddProfile(new AutoMapperProfile(emitter.TypeDescriptions))));
            var queryAccessor = A.Fake<IQueryAccessor>();
            fixture.Inject(queryAccessor);
            var dataProvider = new DataProvider(queryAccessor, mapper, new MemoryCache(Options.Create(new MemoryCacheOptions())));
            fixture.Inject(dataProvider);
        }
    }
}