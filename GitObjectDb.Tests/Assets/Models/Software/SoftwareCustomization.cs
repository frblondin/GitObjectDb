using AutoFixture;
using AutoFixture.Kernel;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Tests.Assets.Models.Software
{
    public class SoftwareCustomization : ICustomization
    {
        public const int DefaultApplicationCount = 2;
        public const int DefaultTablePerApplicationCount = 3;
        public const int DefaultFieldPerTableCount = 10;

        public SoftwareCustomization()
            : this(DefaultApplicationCount, DefaultTablePerApplicationCount, DefaultFieldPerTableCount)
        {
        }

        public SoftwareCustomization(int applicationCount, int tablePerApplicationCount, int fieldPerTableCount, string repositoryPath = null)
        {
            ApplicationCount = applicationCount;
            TablePerApplicationCount = tablePerApplicationCount;
            FieldPerTableCount = fieldPerTableCount;
            RepositoryPath = repositoryPath;
        }

        public string RepositoryPath { get; }

        public int ApplicationCount { get; }

        public int TablePerApplicationCount { get; }

        public int FieldPerTableCount { get; }

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
                var result = (IConnectionInternal)repositoryFactory(path);
                var transformations = result.Update(CreateApplications);
                transformations.Commit(
                    fixture.Create<string>(),
                    fixture.Create<Signature>(),
                    fixture.Create<Signature>());
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
                        composer = composer.Create(application, null);
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
                    composer = composer.Create(table, parent: application);
                    composer = CreateFields(table, composer);
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
                    composer = composer.Create(field, parent: table);
                });
                return composer;
            }

            fixture.Register(PickFirstApplication);
            fixture.Register(PickRandomTable);
            fixture.Register(PickRandomField);

            Application PickFirstApplication() => fixture.Create<IConnection>().GetApplications().First();
            Table PickRandomTable() => PickFirstApplication().GetTables(fixture.Create<IConnection>()).First();
            Field PickRandomField() => PickRandomTable().GetFields(fixture.Create<IConnection>()).First();
        }
    }
}
