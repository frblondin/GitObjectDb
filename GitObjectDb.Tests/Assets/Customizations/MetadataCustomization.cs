using AutoFixture;
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
        readonly int _applicationCount = 10;
        readonly int _pagePerApplicationCount = 5;
        readonly int _fieldPerPageCount = 20;

        public void Customize(IFixture fixture)
        {
            var serviceProvider = fixture.Create<IServiceProvider>();
            var module = new Instance(serviceProvider, Guid.NewGuid(), "Some module", new LazyChildren<Application>(
                Enumerable.Range(1, _applicationCount).Select(a =>
                    new Application(serviceProvider, Guid.NewGuid(), $"Application {a}", new LazyChildren<Page>(
                        Enumerable.Range(1, _pagePerApplicationCount).Select(p =>
                            new Page(serviceProvider, Guid.NewGuid(), $"Page {p}", $"Description for {p}", new LazyChildren<Field>(
                                Enumerable.Range(1, _fieldPerPageCount).Select(f =>
                                    new Field(serviceProvider, Guid.NewGuid(), $"Field {f}"))
                                .ToImmutableList())))
                        .ToImmutableList())))
                .ToImmutableList()));
            fixture.Inject(module);

            var random = new Random();
            fixture.Register(() => module.Applications[random.Next(_applicationCount)]);
            fixture.Register(() => fixture.Create<Application>().Pages[random.Next(_pagePerApplicationCount)]);
            fixture.Register(() => fixture.Create<Page>().Fields[random.Next(_fieldPerPageCount)]);
        }
    }
}
