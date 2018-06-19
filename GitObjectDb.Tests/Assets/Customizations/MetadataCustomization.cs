using AutoFixture;
using GitObjectDb.Models;
using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using GitObjectDb.Tests.Assets.Models;

namespace GitObjectDb.Tests.Assets.Customizations
{
    public class MetadataCustomization : ICustomization
    {
        int ApplicationCount = 10;
        int PagePerApplicationCount = 5;
        int FieldPerPageCount = 20;

        public void Customize(IFixture fixture)
        {
            var factory = fixture.Create<Instance.Factory>();
            var module = factory(Guid.NewGuid(), "Some module", new LazyChildren<Application>(
                Enumerable.Range(1, ApplicationCount).Select(a =>
                    new Application(Guid.NewGuid(), $"Application {a}", new LazyChildren<Page>(
                        Enumerable.Range(1, PagePerApplicationCount).Select(p =>
                            new Page(Guid.NewGuid(), $"Page {p}", new LazyChildren<Field>(
                                Enumerable.Range(1, FieldPerPageCount).Select(f =>
                                    new Field(Guid.NewGuid(), $"Field {f}"))
                                .Order().ToImmutableList())))
                        .Order().ToImmutableList())))
                .Order().ToImmutableList()));
            fixture.Inject(module);

            var random = new Random();
            fixture.Register(() => module.Applications[random.Next(ApplicationCount)]);
            fixture.Register(() => fixture.Create<Application>().Pages[random.Next(PagePerApplicationCount)]);
            fixture.Register(() => fixture.Create<Page>().Fields[random.Next(FieldPerPageCount)]);
        }
    }
}
