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
            Lazy<ObjectRepository> module = default;
            var serviceProvider = fixture.Create<IServiceProvider>();
            var createdPages = new List<Page>();

            Application PickRandomApplication() => module.Value.Applications.PickRandom();
            Page PickRandomPage(Func<Page, bool> predicate) =>
                !createdPages.Any() ?
                module.Value.Applications.PickRandom().Pages.Last(predicate) :
                createdPages.Last(predicate);
            Field PickRandomField() => PickRandomPage(_ => true).Fields.PickRandom();
            LinkField PickRandomLinkField() => PickRandomPage(p => p.Fields.OfType<LinkField>().Any()).Fields.OfType<LinkField>().First();
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

            module = new Lazy<ObjectRepository>(() => new ObjectRepository(serviceProvider, Guid.NewGuid(), "Some repository", new LazyChildren<IMigration>(), new LazyChildren<Application>(
                Enumerable.Range(1, ApplicationCount).Select(a =>
                    new Application(serviceProvider, Guid.NewGuid(), $"Application {a}", new LazyChildren<Page>(
                        Enumerable.Range(1, PagePerApplicationCount).Select(p =>
                            CreatePage(p))
                        .ToImmutableList())))
                .ToImmutableList())));

            fixture.Register(PickRandomApplication);
            fixture.Register(() => PickRandomPage(_ => true));
            fixture.Register(PickRandomField);
            fixture.Register(PickRandomLinkField);
            fixture.Register(() => module.Value);
        }
    }
}
