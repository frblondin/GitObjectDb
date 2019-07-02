using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using AutoFixture;
using GitObjectDb.Models;
using GitObjectDb.Models.Migration;
using GitObjectDb.Tests.Assets.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GitObjectDb.Tests.Assets.Customizations
{
    public class BlobCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Register(UniqueId.CreateNew);

            var serviceProvider = fixture.Create<IServiceProvider>();
            var containerFactory = serviceProvider.GetRequiredService<IObjectRepositoryContainerFactory>();

            var container = containerFactory.Create<BlobRepository>(RepositoryFixture.GetAvailableFolderPath());
            fixture.Inject(container);
            fixture.Inject<IObjectRepositoryContainer<BlobRepository>>(container);
            fixture.Inject<IObjectRepositoryContainer>(container);

            Car CreateCar(int position) =>
                new Car.Builder(serviceProvider)
                {
                    Id = UniqueId.CreateNew(),
                    Name = $"Car {position}",
                    Blob = new StringBlob("Car blob")
                }.ToImmutable();
            fixture.Register(() => new BlobRepository(serviceProvider, container,
                UniqueId.CreateNew(),
                "Some repository",
                new Version(1, 0, 0),
                ImmutableList.Create<RepositoryDependency>(),
                new LazyChildren<IMigration>(),
                new LazyChildren<IObjectRepositoryIndex>(),
                new StringBlob("a\nb\nc"),
                new LazyChildren<Car>(ImmutableList.Create(CreateCar(1)))));
        }
    }
}
