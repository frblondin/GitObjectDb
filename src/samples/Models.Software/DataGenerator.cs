using AutoFixture;
using GitObjectDb;
using LibGit2Sharp;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Models.Software;

public class DataGenerator
{
    public const int DefaultApplicationCount = 2;
    public const int DefaultTablePerApplicationCount = 3;
    public const int DefaultFieldPerTableCount = 10;
    public const int DefaultConstantPerTableCount = 2;
    public const int DefaultResourcePerTableCount = 5;

    public DataGenerator(IConnection connection, int applicationCount = DefaultApplicationCount, int tablePerApplicationCount = DefaultTablePerApplicationCount, int fieldPerTableCount = DefaultFieldPerTableCount, int constantPerTableCount = DefaultConstantPerTableCount, int resourcePerTableCount = DefaultResourcePerTableCount)
    {
        Connection = connection;
        ApplicationCount = applicationCount;
        TablePerApplicationCount = tablePerApplicationCount;
        FieldPerTableCount = fieldPerTableCount;
        ConstantPerTableCount = constantPerTableCount;
        ResourcePerTableCount = resourcePerTableCount;
    }

    public IConnection Connection { get; }

    public int ApplicationCount { get; }

    public int TablePerApplicationCount { get; }

    public int FieldPerTableCount { get; }

    public int ConstantPerTableCount { get; }

    public int ResourcePerTableCount { get; }

    public void CreateData(string commitMessage, Signature signature)
    {
        var fixture = new Fixture();
        var transformations = Connection.Update(CreateApplications);
        transformations.Commit(commitMessage, signature, signature);

        void CreateApplications(ITransformationComposer composer)
        {
            Enumerable.Range(1, ApplicationCount).ForEach(position =>
            {
                var application = composer.CreateOrUpdate(new Application
                {
                    Description = fixture.Create<string>(),
                    Name = fixture.Create<string>(),
                });
                CreateTables(application, composer);
            });
        }

        void CreateTables(Application application, ITransformationComposer composer)
        {
            Enumerable.Range(1, TablePerApplicationCount).ForEach(position =>
            {
                var table = composer.CreateOrUpdate(new Table
                {
                    Description = fixture.Create<string>(),
                    Name = fixture.Create<string>(),
                }, parent: application);
                CreateFields(table, composer);
                CreateConstants(table, composer);
                CreateResource(table, composer);
            });
        }

        void CreateFields(Table table, ITransformationComposer composer)
        {
            Enumerable.Range(1, FieldPerTableCount).ForEach(position =>
            {
                composer.CreateOrUpdate(new Field
                {
                    A = fixture.Create<NestedA[]>(),
                    SomeValue = fixture.Create<NestedA>(),
                }, parent: table);
            });
        }

        void CreateConstants(Table table, ITransformationComposer composer)
        {
            Enumerable.Range(1, ConstantPerTableCount).ForEach(position =>
            {
                composer.CreateOrUpdate(new Constant
                {
                    EmbeddedResource = fixture.Create<string>(),
                }, parent: table);
            });
        }

        void CreateResource(Table table, ITransformationComposer composer)
        {
            Enumerable.Range(1, ResourcePerTableCount).ForEach(position =>
            {
                var stream = new MemoryStream(Encoding.Default.GetBytes(fixture.Create<string>()));
                var resource = new Resource(table,
                                            $"Path{UniqueId.CreateNew()}",
                                            $"File{UniqueId.CreateNew()}.txt",
                                            new Resource.Data(stream));
                composer.CreateOrUpdate(resource);
            });
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
