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
            var module = new ObjectRepository(serviceProvider, Guid.NewGuid(), "Some repository", new LazyChildren<IMigration>(), new LazyChildren<Application>(
                Enumerable.Range(1, ApplicationCount).Select(a =>
                    new Application(serviceProvider, Guid.NewGuid(), $"Application {a}", new LazyChildren<Page>(
                        Enumerable.Range(1, PagePerApplicationCount).Select(p =>
                            new Page(serviceProvider, Guid.NewGuid(), $"Page {p}", $"Description for {p}", new LazyChildren<Field>(
                                Enumerable.Range(1, FieldPerPageCount).Select(f =>
                                    new Field(serviceProvider, Guid.NewGuid(), $"Field {f}"))
                                .ToImmutableList())))
                        .ToImmutableList())))
                .ToImmutableList()));
            fixture.Inject(module);

            fixture.Register(() => module.Value.Applications.PickRandom());
            fixture.Register(() => fixture.Create<Application>().Pages.PickRandom());
            fixture.Register(() => fixture.Create<Page>().Fields.PickRandom());
        }
    }
}
