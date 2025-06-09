using AutoFixture;
using AutoMapper;
using FakeItEasy;
using Fasterflect;
using GitDotNet;
using GitObjectDb.Api.OData.Model;
using GitObjectDb.Api.OData.Tests.Model;
using GitObjectDb.Tests.Assets.Tools;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static GitObjectDb.Api.OData.Tests.Model.BasicModel;

namespace GitObjectDb.Api.OData.Tests;

public class DataProviderTests
{
    [Test]
    public async Task QuerySimpleNodes()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var nodes = fixture.Create<SimpleNode[]>();
        var connection = fixture.Create<IConnection>();
        var commit = fixture.Create<CommitEntry>();
        var typeProvider = fixture.Create<IApplicationPartTypeProvider>();
        var dataProvider = fixture.Create<DataProvider>();

        A.CallTo(() => connection.GetNodesAsync<SimpleNode>(commit, default, false, A<CancellationToken>._))
            .Returns(nodes.ToAsyncEnumerable());
        var simpleNodeDto = GetDtoDescription<SimpleNode>(typeProvider, dataProvider);

        // Act
        var result = (await dataProvider.GetNodesAsync<SimpleNode>(simpleNodeDto, commit)).ToList();

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
    public async Task QueryNodesAndReferences()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var nodes = fixture.Create<MultiReferenceNode[]>();
        var commit = fixture.Create<CommitEntry>();
        var connection = fixture.Create<IConnection>();
        var typeProvider = fixture.Create<IApplicationPartTypeProvider>();
        var dataProvider = fixture.Create<DataProvider>();

        A.CallTo(() => connection.GetNodesAsync<MultiReferenceNode>(commit, default, default, A<CancellationToken>._))
            .Returns(nodes.ToAsyncEnumerable());
        var multiReferenceNodeDto = GetDtoDescription<MultiReferenceNode>(typeProvider, dataProvider);

        // Act
        var result = (await dataProvider.GetNodesAsync<MultiReferenceNode>(multiReferenceNodeDto, commit)).ToList();

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
    public async Task QueryNodeChildren()
    {
        // Arrange
        var fixture = new Fixture().Customize(new Customization());
        var node = fixture.Create<SimpleNode>();
        var commit = fixture.Create<CommitEntry>();
        var children = fixture.Create<MultiReferenceNode[]>();
        var queryAccessor = fixture.Create<IConnection>();
        var typeProvider = fixture.Create<IApplicationPartTypeProvider>();
        var dataProvider = fixture.Create<DataProvider>();

        A.CallTo(() => queryAccessor.GetNodesAsync<SimpleNode>(commit, default, default, A<CancellationToken>._))
            .Returns(Enumerable.Repeat(node, 1).ToAsyncEnumerable());
        A.CallTo(() => queryAccessor.GetNodesAsync<Node>(A<CommitEntry>._, node, false, A<CancellationToken>._))
            .Returns(children.ToAsyncEnumerable());
        var simpleNodeDto = GetDtoDescription<SimpleNode>(typeProvider, dataProvider);

        // Act
        var result = (await dataProvider.GetNodesAsync<SimpleNode>(simpleNodeDto, commit)).ToList();
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
            fixture.Register<HashId>(() => null);
            fixture.Inject(GetCommit());

            var emitter = new DtoTypeEmitter(CreateDataModel(typeof(BasicModel).GetNestedTypes()));
            var part = new GeneratedTypesApplicationPart(emitter);
            fixture.Inject<IApplicationPartTypeProvider>(part);
            var mapper = new Mapper(
                new MapperConfiguration(
                    c => c.AddProfile(new AutoMapperProfile(emitter.TypeDescriptions))));
            var connection = A.Fake<IConnection>();
            fixture.Inject(connection);
            var dataProvider = new DataProvider(connection, mapper, new MemoryCache(Options.Create(new MemoryCacheOptions())));
            fixture.Inject(dataProvider);
        }

        private static CommitEntry GetCommit()
        {
            using var connection = new ServiceCollection()
                .AddMemoryCache()
                .AddGitDotNet()
                .BuildServiceProvider()
                .GetRequiredService<GitConnectionProvider>()
                .Invoke(".");
            return AsyncHelper.RunSync(() => connection.GetCommittishAsync("HEAD"));
        }
    }
}