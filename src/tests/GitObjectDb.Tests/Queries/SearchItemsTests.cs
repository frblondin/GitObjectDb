using AutoFixture;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Linq;

namespace GitObjectDb.Tests.Queries;

[Parallelizable(ParallelScope.Self | ParallelScope.Children)]
public class SearchItemsTests : DisposeArguments
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(Customization))]
    public void SearchStringPropertiesByDefault(IConnection connection, SomeNode node)
    {
        // Act
        var result = connection.Search("main", pattern: node.SearchableByDefault).ToList();

        // Assert
        Assert.That(result, Has.Exactly(1).Items);
        Assert.That(result[0], Is.EqualTo(node));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(Customization))]
    public void SearchExplicitelySearchableProperties(IConnection connection, SomeNode node)
    {
        // Act
        var result = connection.Search("main", pattern: node.SearchableExplicitely.ToString(), ignoreCase: true).ToList();

        // Assert
        Assert.That(result, Has.Some.Not.Null);
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(Customization))]
    public void SkipExcludedProperties(IConnection connection, SomeNode node)
    {
        // Act
        var result = connection.Search("main", pattern: node.NonSearchable).ToList();

        // Assert
        Assert.That(result, Has.Exactly(0).Items);
    }

    private class Customization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var model = new ConventionBaseModelBuilder().RegisterType<SomeNode>().Build();
            fixture.Do<IServiceCollection>(services => services.AddSingleton(model));
            var connection = new Lazy<IConnectionInternal>(() =>
            {
                var serviceProvider = fixture.Create<IServiceProvider>();
                return CreateConnection(fixture, serviceProvider, model);
            });

            fixture.Register(() => connection.Value);
            fixture.Register<IConnection>(() => connection.Value);
            fixture.Register(() => connection.Value.Repository);

            fixture.LazyRegister(() => connection.Value.GetNodes<SomeNode>("main").Last());
        }

        private static IConnectionInternal CreateConnection(IFixture fixture, IServiceProvider serviceProvider, IDataModel model)
        {
            var path = GitObjectDbFixture.GetAvailableFolderPath();
            var repositoryFactory = serviceProvider.GetRequiredService<ConnectionFactory>();
            var result = (IConnectionInternal)repositoryFactory(path, model);
            var transformations = result.Update("main", c =>
            {
                for (int i = 0; i < 10; i++)
                {
                    c.CreateOrUpdate(new SomeNode
                    {
                        SearchableByDefault = fixture.Create<string>(),
                        SearchableExplicitely = StringComparison.OrdinalIgnoreCase,
                        NonSearchable = fixture.Create<string>(),
                    });
                }
            });
            transformations.Commit(new(fixture.Create<string>(),
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
