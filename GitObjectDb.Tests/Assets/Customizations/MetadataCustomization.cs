using AutoFixture;
using GitObjectDb.Migrations;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Models;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GitObjectDb.Tests.Assets.Customizations
{
    public class MetadataCustomization : ICustomization
    {
        public const int DefaultApplicationCount = 2;
        public const int DefaultPagePerApplicationCount = 3;
        public const int DefaultFieldPerPageCount = 10;

        public MetadataCustomization()
            : this(DefaultApplicationCount, DefaultPagePerApplicationCount, DefaultFieldPerPageCount)
        {
        }

        public MetadataCustomization(int applicationCount, int pagePerApplicationCount, int fieldPerPageCount)
        {
            ApplicationCount = applicationCount;
            PagePerApplicationCount = pagePerApplicationCount;
            FieldPerPageCount = fieldPerPageCount;
        }

        public int ApplicationCount { get; }

        public int PagePerApplicationCount { get; }

        public int FieldPerPageCount { get; }

        public void Customize(IFixture fixture)
        {
            var serviceProvider = fixture.Create<IServiceProvider>();

            var tempPath = RepositoryFixture.GetRepositoryPath(fixture.Create<Guid>().ToString());
            var container = new ObjectRepositoryContainer<ObjectRepository>(serviceProvider, tempPath);
            fixture.Inject(container);
            fixture.Inject<IObjectRepositoryContainer<ObjectRepository>>(container);
            fixture.Inject<IObjectRepositoryContainer>(container);

            ObjectRepository lastModule = default;
            var createdPages = new List<Page>();

            Page CreatePage(int position)
            {
                var page = new Page(serviceProvider, Guid.NewGuid(), $"Page {position}", $"Description for {position}", new LazyChildren<Field>(
                    Enumerable.Range(1, FieldPerPageCount).Select(f =>
                        CreateField(f))
                    .ToImmutableList()));
                createdPages.Add(page);
                return page;
            }
            Field CreateField(int position) =>
                createdPages.Any() && position % 3 == 0 ?
                new LinkField(serviceProvider, Guid.NewGuid(), $"Field {position}", new LazyLink<Page>(PickRandomPage(_ => true))) :
                new Field(serviceProvider, Guid.NewGuid(), $"Field {position}");

            ObjectRepository CreateModule()
            {
                createdPages.Clear();
                lastModule = new ObjectRepository(serviceProvider, container, Guid.NewGuid(), "Some repository", new Version(1, 0, 0), ImmutableList.Create<RepositoryDependency>(), new LazyChildren<IMigration>(), new LazyChildren<Application>(
                    Enumerable.Range(1, ApplicationCount).Select(a =>
                        new Application(serviceProvider, Guid.NewGuid(), $"Application {a}", new LazyChildren<Page>(
                            Enumerable.Range(1, PagePerApplicationCount).Select(p =>
                                CreatePage(p))
                            .ToImmutableList())))
                    .ToImmutableList()));
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
