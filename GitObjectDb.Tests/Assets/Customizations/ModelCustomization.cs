using AutoFixture;
using GitObjectDb.Models;
using GitObjectDb.Models.Migration;
using GitObjectDb.Tests.Assets.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

            var path = ContainerPath ?? RepositoryFixture.GetAvailableFolderPath();
            var container = new ObjectRepositoryContainer<ObjectRepository>(serviceProvider, path);
            fixture.Inject(container);
            fixture.Inject<IObjectRepositoryContainer<ObjectRepository>>(container);
            fixture.Inject<IObjectRepositoryContainer>(container);

            ObjectRepository lastModule = default;
            var createdPages = new List<Page>();

            Page CreatePage(int position)
            {
                var page = new Page(serviceProvider, UniqueId.CreateNew(), $"Page {position}", $"Description for {position}", new LazyChildren<Field>(
                    Enumerable.Range(1, FieldPerPageCount).Select(f =>
                        CreateField(f))
                    .OrderBy(f => f.Id).ToImmutableList()));
                createdPages.Add(page);
                return page;
            }
            Field CreateField(int position) =>
                createdPages.Any() && position % 3 == 0 ?
                new LinkField(serviceProvider, UniqueId.CreateNew(), $"Field {position}", new LazyLink<Page>(PickRandomPage(_ => true))) :
                new Field(serviceProvider, UniqueId.CreateNew(), $"Field {position}");

            ObjectRepository CreateModule()
            {
                createdPages.Clear();
                lastModule = new ObjectRepository(serviceProvider, container, UniqueId.CreateNew(), "Some repository", new Version(1, 0, 0), ImmutableList.Create<RepositoryDependency>(), new LazyChildren<IMigration>(), new LazyChildren<Application>(
                    Enumerable.Range(1, ApplicationCount).Select(a =>
                        new Application(serviceProvider, UniqueId.CreateNew(), $"Application {a}", new LazyChildren<Page>(
                            Enumerable.Range(1, PagePerApplicationCount).Select(p =>
                                CreatePage(p))
                            .OrderBy(p => p.Id).ToImmutableList())))
                    .OrderBy(a => a.Id).ToImmutableList()));
                return lastModule;
            }

            Application PickRandomApplication() => (lastModule ?? CreateModule()).Applications.PickRandom();
            Page PickRandomPage(Func<Page, bool> predicate) =>
                !createdPages.Any() ?
                PickRandomApplication().Pages.Last(predicate) :
                createdPages.Last(predicate);
            Field PickRandomField() => PickRandomPage(_ => true).Fields.PickRandom();
            LinkField PickRandomLinkField() => PickRandomPage(p => p.Fields.OfType<LinkField>().Any()).Fields.OfType<LinkField>().First();

            fixture.Register(PickRandomApplication);
                fixture.Register(() => PickRandomPage(_ => true));
                fixture.Register(PickRandomField);
                fixture.Register(PickRandomLinkField);
                fixture.Register(CreateModule);
        }
    }
}
