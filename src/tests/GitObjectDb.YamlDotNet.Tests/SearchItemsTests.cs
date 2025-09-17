using AutoFixture;
using GitDotNet;
using GitObjectDb.Model;
using GitObjectDb.Tests;
using GitObjectDb.Tests.Assets;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.YamlDotNet.Tests;

public class SearchItemsTests
{
    [Test]
    public async Task SearchStringPropertiesByDefault()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).CustomizeAsync<Customization>();
        using var connection = fixture.Create<IConnection>();
        var node = fixture.Create<SomeNode>();

        // Act
        var mainTip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.SearchAsync(mainTip, pattern: node.SearchableByDefault).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Exactly(1).Items);
        Assert.That(result[0], Is.EqualTo(node));
    }

    [Test]
    public async Task ThrowExceptionForCharactersNeedingYamlEscaping()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).CustomizeAsync<Customization>();
        using var connection = fixture.Create<IConnection>();
        var node = fixture.Create<SomeNode>();

        // Act, Assert
        var mainTip = await connection.Repository.GetCommittishAsync("main");
        Assert.Multiple(() =>
        {
            foreach (var c in "\"\\\0")
            {
                Assert.Throws<GitObjectDbException>(
                    () => connection.SearchAsync(mainTip, pattern: c.ToString()).ToEnumerable().ToList());
            }
        });
    }

    [Test]
    public async Task SearchExplicitlySearchableProperties()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).CustomizeAsync<Customization>();
        using var connection = fixture.Create<IConnection>();
        var node = fixture.Create<SomeNode>();

        // Act
        var mainTip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.SearchAsync(mainTip, pattern: node.SearchableExplicitely.ToString(), ignoreCase: true).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Some.Not.Null);
    }

    [Test]
    public async Task SkipExcludedProperties()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultYamlServiceProviderCustomization()).CustomizeAsync<Customization>();
        using var connection = fixture.Create<IConnection>();
        var node = fixture.Create<SomeNode>();

        // Act
        var mainTip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.SearchAsync(mainTip, pattern: node.NonSearchable).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Exactly(0).Items);
    }

    private class Customization : IAsyncCustomization
    {
        public async Task CustomizeAsync(IFixture fixture)
        {
            var model = new ConventionBaseModelBuilder().RegisterType<SomeNode>().Build();
            fixture.Do<IServiceCollection>(services => services.AddSingleton(model));
            var serviceProvider = fixture.Create<IServiceProvider>();
            var connection = await CreateConnectionAsync(fixture, serviceProvider, model);
            var mainTip = await connection.Repository.GetCommittishAsync("main");

            fixture.Register(() => connection);
            fixture.Register(() => connection);
            fixture.Register(() => connection.Repository);

            fixture.LazyRegister(() => connection.GetNodesAsync<SomeNode>(mainTip).ToEnumerable().OrderBy(n => n.Id).Last());
        }

        private static async Task<IConnection> CreateConnectionAsync(IFixture fixture, IServiceProvider serviceProvider, IDataModel model)
        {
            var path = GitObjectDbFixture.GetAvailableFolderPath();
            var repositoryFactory = serviceProvider.GetRequiredService<ConnectionFactory>();
            var result = repositoryFactory(path, model);
            var transformations = await result.UpdateAsync("main", async c =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await c.CreateOrUpdateAsync(new SomeNode
                    {
                        SearchableByDefault = fixture.Create<string>(),
                        SearchableExplicitely = StringComparison.OrdinalIgnoreCase,
                        NonSearchable = fixture.Create<string>(),
                    });
                }
            });
            await transformations.CommitAsync(new(fixture.Create<string>(),
                                       fixture.Create<Signature>(),
                                       fixture.Create<Signature>()));
            return result;
        }
    }

    public record SomeNode : Node
    {
        public string SearchableByDefault { get; init; }

        [IsSearchable]
        public StringComparison SearchableExplicitely { get; init; }

        [IsSearchable(false)]
        public string NonSearchable { get; init; }
    }
}
