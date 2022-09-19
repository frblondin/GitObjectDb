using AutoFixture;
using GitObjectDb.Model;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using Models.Software;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Tests.Assets.Data.Software
{
    public class SoftwareCustomization : ICustomization
    {
        public SoftwareCustomization()
            : this(DataGenerator.DefaultApplicationCount, DataGenerator.DefaultTablePerApplicationCount, DataGenerator.DefaultFieldPerTableCount, DataGenerator.DefaultResourcePerTableCount)
        {
        }

        public SoftwareCustomization(string repositoryPath)
            : this(DataGenerator.DefaultApplicationCount, DataGenerator.DefaultTablePerApplicationCount, DataGenerator.DefaultFieldPerTableCount, DataGenerator.DefaultResourcePerTableCount, repositoryPath)
        {
        }

        public SoftwareCustomization(int applicationCount, int tablePerApplicationCount, int fieldPerTableCount, int resourcePerTableCount, string repositoryPath = null)
        {
            ApplicationCount = applicationCount;
            TablePerApplicationCount = tablePerApplicationCount;
            FieldPerTableCount = fieldPerTableCount;
            ResourcePerTableCount = resourcePerTableCount;
            RepositoryPath = repositoryPath;
        }

        public string RepositoryPath { get; }

        public int ApplicationCount { get; }

        public int TablePerApplicationCount { get; }

        public int FieldPerTableCount { get; }

        public int ResourcePerTableCount { get; }

        public void Customize(IFixture fixture)
        {
            fixture.Register(UniqueId.CreateNew);

            var serviceProvider = fixture.Create<IServiceProvider>();

            var path = RepositoryPath ?? GitObjectDbFixture.GetAvailableFolderPath();
            var repositoryFactory = serviceProvider.GetRequiredService<ConnectionFactory>();
            var connection = new Lazy<IConnectionInternal>(CreateConnection);
            fixture.Register(() => connection.Value);
            fixture.Register<IConnection>(() => connection.Value);
            fixture.Register(() => connection.Value.Repository);

            IConnectionInternal CreateConnection()
            {
                var alreadyExists = Directory.Exists(path);
                var model = serviceProvider.GetRequiredService<IDataModel>();
                var result = (IConnectionInternal)repositoryFactory(path, model);
                if (!alreadyExists)
                {
                    var software = new DataGenerator(result, ApplicationCount, TablePerApplicationCount, FieldPerTableCount, ResourcePerTableCount);
                    software.CreateData(fixture.Create<string>(), fixture.Create<Signature>());
                }
                return result;
            }

            fixture.Register(PickFirstApplication);
            fixture.Register(PickRandomTable);
            fixture.Register(PickRandomField);
            fixture.Register(PickRandomResource);

            Application PickFirstApplication() => fixture.Create<IConnection>().GetApplications().Last();
            Table PickRandomTable() => fixture.Create<IConnection>().GetTables(PickFirstApplication()).Last();
            Field PickRandomField() => fixture.Create<IConnection>().GetFields(PickRandomTable()).Last();
            Resource PickRandomResource() => fixture.Create<IConnection>().GetResources(PickRandomTable()).Last();
        }
    }
}
