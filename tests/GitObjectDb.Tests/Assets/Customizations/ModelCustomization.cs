using AutoFixture;
using AutoFixture.Kernel;
using GitObjectDb.Models;
using GitObjectDb.Models.Migration;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Models;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Assets.Customizations
{
    public class ModelCustomization : ICustomization
    {
        public const int DefaultApplicationCount = 2;
        public const int DefaultPagePerApplicationCount = 3;
        public const int DefaultFieldPerPageCount = 10;

        public ModelCustomization()
            : this(DefaultApplicationCount, DefaultPagePerApplicationCount, DefaultFieldPerPageCount)
        {
        }

        public ModelCustomization(int applicationCount, int pagePerApplicationCount, int fieldPerPageCount, string containerPath = null)
        {
            ApplicationCount = applicationCount;
            PagePerApplicationCount = pagePerApplicationCount;
            FieldPerPageCount = fieldPerPageCount;
            ContainerPath = containerPath;
        }

        public string ContainerPath { get; }

        public int ApplicationCount { get; }

        public int PagePerApplicationCount { get; }

        public int FieldPerPageCount { get; }

        public void Customize(IFixture fixture)
        {
            fixture.Register(UniqueId.CreateNew);

            var serviceProvider = fixture.Create<IServiceProvider>();
            var containerFactory = serviceProvider.GetRequiredService<IObjectRepositoryContainerFactory>();

            var path = ContainerPath ?? RepositoryFixture.GetAvailableFolderPath();
            fixture.Customizations.Add(new ObjectRepositoryContainerSpecimenBuilder(path));
            var container = fixture.Freeze<IObjectRepositoryContainer<ObjectRepository>>();

            ObjectRepository lastRepository = default;
            var createdPages = new List<Page>();

            Page CreatePage(int position)
            {
                var page = new Page.Builder(serviceProvider)
                {
                    Id = UniqueId.CreateNew(),
                    Name = $"Page {position}",
                    Description = $"Description for {position}",
                    Fields = new LazyChildren<Field>(
                        _ => Task.FromResult<IImmutableList<Field>>(Enumerable.Range(1, FieldPerPageCount).Select(f =>
                            CreateField(f))
                        .OrderBy(f => f.Id).ToImmutableList()))
                }.ToImmutable();
                createdPages.Add(page);
                return page;
            }
            Field CreateField(int position) =>
                new Field.Builder(serviceProvider)
                {
                    Id = UniqueId.CreateNew(),
                    Name = $"Field {position}",
                    Content = createdPages.Any() && position % 3 == 0 ?
                              FieldContent.NewLink(new FieldLinkContent(new LazyLink<Page>(container, PickRandomPage(_ => true)))) :
                              FieldContent.Default
                }.ToImmutable();

            ObjectRepository CreateModule()
            {
                createdPages.Clear();
                lastRepository = new ObjectRepository(serviceProvider, container,
                    UniqueId.CreateNew(),
                    "Some repository",
                    new Version(1, 0, 0),
                    ImmutableList.Create<RepositoryDependency>(),
                    new LazyChildren<IMigration>(),
                    new LazyChildren<IObjectRepositoryIndex>(
                        _ => Task.FromResult<IImmutableList<IObjectRepositoryIndex>>(ImmutableList.Create<IObjectRepositoryIndex>(new LinkFieldReferrerIndex(serviceProvider, UniqueId.CreateNew(), nameof(LinkFieldReferrerIndex))))),
                    new LazyChildren<Application>(
                        _ => Task.FromResult<IImmutableList<Application>>(Enumerable.Range(1, ApplicationCount).Select(a =>
                            new Application(serviceProvider, UniqueId.CreateNew(), $"Application {a}", new LazyChildren<Page>(
                                _ => Task.FromResult<IImmutableList<Page>>(Enumerable.Range(1, PagePerApplicationCount).Select(p =>
                                    CreatePage(p))
                                .OrderBy(p => p.Id).ToImmutableList()))))
                        .OrderBy(a => a.Id).ToImmutableList())));
                return lastRepository;
            }

            Application PickRandomApplication()
            {
                var applications = AsyncContext.Run(() => (lastRepository ?? CreateModule()).Applications.Task);
                return applications.PickRandom();
            }

            Page PickRandomPage(Func<Page, bool> predicate)
            {
                var pages = AsyncContext.Run(() => PickRandomApplication().Pages.Task);
                return !createdPages.Any() ?
                    pages.Last(predicate) :
                    createdPages.Last(predicate);
            }

            Field PickRandomField()
            {
                var fields = AsyncContext.Run(() => PickRandomPage(_ => true).Fields.Task);
                return fields.PickRandom();
            }

            fixture.Register(PickRandomApplication);
            fixture.Register(() => PickRandomPage(_ => true));
            fixture.Register(PickRandomField);
            fixture.Register(CreateModule);
        }

        private class ObjectRepositoryContainerSpecimenBuilder : ISpecimenBuilder
        {
            private readonly string _path;
            private static readonly MethodInfo _factoryCreate = ExpressionReflector.GetMethod<ObjectRepositoryContainerSpecimenBuilder>(
                f => f.CreateContainer<ObjectRepository>(null), true);

            public ObjectRepositoryContainerSpecimenBuilder(string path)
            {
                _path = path ?? throw new ArgumentNullException(nameof(path));
            }

            public object Create(object request, ISpecimenContext context)
            {
                if (request is Type type)
                {
                    if (type == typeof(IObjectRepositoryContainer))
                    {
                        type = typeof(IObjectRepositoryContainer<ObjectRepository>);
                    }
                    if (type.IsGenericType &&
                        (type.GetGenericTypeDefinition() == typeof(IObjectRepositoryContainer<>) ||
                        type.GetGenericTypeDefinition() == typeof(ObjectRepositoryContainer<>)))
                    {
                        var serviceProvider = context.Create<IServiceProvider>();
                        var containerFactory = serviceProvider.GetRequiredService<IObjectRepositoryContainerFactory>();
                        var repositoryType = type.GetGenericArguments()[0];
                        return _factoryCreate.MakeGenericMethod(repositoryType)
                            .Invoke(this, new object[] { containerFactory });
                    }
                }
                return new NoSpecimen();
            }

            private IObjectRepositoryContainer CreateContainer<TRepository>(IObjectRepositoryContainerFactory factory)
                where TRepository : class, IObjectRepository
            {
                return AsyncContext.Run(() => factory.CreateAsync<TRepository>(_path));
            }
        }
    }
}
