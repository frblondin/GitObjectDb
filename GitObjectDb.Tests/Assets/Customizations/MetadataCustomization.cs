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
        internal const int ApplicationCount = 2;
        internal const int PagePerApplicationCount = 3;
        internal const int FieldPerPageCount = 10;

        public void Customize(IFixture fixture)
        {
            var serviceProvider = fixture.Create<IServiceProvider>();
            var module = new Instance(serviceProvider, Guid.NewGuid(), "Some module", new LazyChildren<IMigration>(), new LazyChildren<Application>(
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

            var random = new Random();
            fixture.Register(() => module.Applications[random.Next(ApplicationCount)]);
            fixture.Register(() => fixture.Create<Application>().Pages[random.Next(PagePerApplicationCount)]);
            fixture.Register(() => fixture.Create<Page>().Fields[random.Next(FieldPerPageCount)]);
        }
    }
}
