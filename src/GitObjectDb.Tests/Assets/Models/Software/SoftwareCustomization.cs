using AutoFixture;
using AutoFixture.Kernel;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models.Software
{
    public class SoftwareCustomization : ICustomization
    {
        public const int DefaultApplicationCount = 2;
        public const int DefaultTablePerApplicationCount = 3;
        public const int DefaultFieldPerTableCount = 10;
        public const int DefaultResourcePerTableCount = 5;

        private Random _random = new Random();

        public SoftwareCustomization()
            : this(DefaultApplicationCount, DefaultTablePerApplicationCount, DefaultFieldPerTableCount, DefaultResourcePerTableCount)
        {
        }

        public SoftwareCustomization(string repositoryPath)
            : this(DefaultApplicationCount, DefaultTablePerApplicationCount, DefaultFieldPerTableCount, DefaultResourcePerTableCount, repositoryPath)
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
            fixture.Register<Repository>(() => connection.Value.Repository);

            IConnectionInternal CreateConnection()
            {
                var alreadyExists = Directory.Exists(path);
                var result = (IConnectionInternal)repositoryFactory(path);
                if (!alreadyExists)
                {
                    var transformations = result.Update(CreateApplications);
                    var signature = fixture.Create<Signature>();
                    transformations.Commit(fixture.Create<string>(), signature, signature);
                }
                return result;
            }

            INodeTransformationComposer CreateApplications(INodeTransformationComposer composer)
            {
                Enumerable.Range(1, ApplicationCount).ForEach(position =>
                {
                    var application = new Application(fixture.Create<UniqueId>())
                    {
                        Description = fixture.Create<string>(),
                        Name = fixture.Create<string>(),
                    };
                    composer = composer.CreateOrUpdate(application, null);
                    composer = CreateTables(application, composer);
                });
                return composer;
            }

            INodeTransformationComposer CreateTables(Application application, INodeTransformationComposer composer)
            {
                Enumerable.Range(1, TablePerApplicationCount).ForEach(position =>
                {
                    var table = new Table(fixture.Create<UniqueId>())
                    {
                        Description = fixture.Create<string>(),
                        Name = fixture.Create<string>(),
                    };
                    composer = composer.CreateOrUpdate(table, parent: application);
                    composer = CreateFields(table, composer);
                    composer = CreateResource(table, composer);
                });
                return composer;
            }

            INodeTransformationComposer CreateFields(Table table, INodeTransformationComposer composer)
            {
                Enumerable.Range(1, FieldPerTableCount).ForEach(position =>
                {
                    var field = new Field(fixture.Create<UniqueId>())
                    {
                        A = fixture.Create<NestedA[]>(),
                        SomeValue = fixture.Create<NestedA>(),
                    };
                    composer = composer.CreateOrUpdate(field, parent: table);
                });
                return composer;
            }

            INodeTransformationComposer CreateResource(Table table, INodeTransformationComposer composer)
            {
                Enumerable.Range(1, ResourcePerTableCount).ForEach(position =>
                {
                    var path = new DataPath(
                        $"Path{_random.Next(1, 2)}",
                        $"File{_random.Next(1, 100)}.txt");
                    var resource = new Resource(table, path, Encoding.Default.GetBytes(fixture.Create<string>()));
                    composer = composer.CreateOrUpdate(resource);
                });
                return composer;
            }

            fixture.Register(PickFirstApplication);
            fixture.Register(PickRandomTable);
            fixture.Register(PickRandomField);
            fixture.Register(PickRandomResource);

            Application PickFirstApplication() => fixture.Create<IConnection>().GetApplications().Last();
            Table PickRandomTable() => PickFirstApplication().GetTables(fixture.Create<IConnection>()).Last();
            Field PickRandomField() => PickRandomTable().GetFields(fixture.Create<IConnection>()).Last();
            Resource PickRandomResource() => fixture.Create<IConnection>().GetResources(PickRandomTable()).Last();
        }
    }
}
