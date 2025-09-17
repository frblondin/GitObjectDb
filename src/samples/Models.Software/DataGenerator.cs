using AutoFixture;
using GitDotNet;
using GitObjectDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Software;

public class DataGenerator(IConnection connection,
    int applicationCount = DataGenerator.DefaultApplicationCount,
    int tablePerApplicationCount = DataGenerator.DefaultTablePerApplicationCount,
    int fieldPerTableCount = DataGenerator.DefaultFieldPerTableCount,
    int constantPerTableCount = DataGenerator.DefaultConstantPerTableCount,
    int resourcePerTableCount = DataGenerator.DefaultResourcePerTableCount)
{
    public const int DefaultApplicationCount = 2;
    public const int DefaultTablePerApplicationCount = 3;
    public const int DefaultFieldPerTableCount = 10;
    public const int DefaultConstantPerTableCount = 2;
    public const int DefaultResourcePerTableCount = 5;

    public IConnection Connection { get; } = connection;

    public int ApplicationCount { get; } = applicationCount;

    public int TablePerApplicationCount { get; } = tablePerApplicationCount;

    public int FieldPerTableCount { get; } = fieldPerTableCount;

    public int ConstantPerTableCount { get; } = constantPerTableCount;

    public int ResourcePerTableCount { get; } = resourcePerTableCount;

    public async Task<CommitEntry> CreateDataAsync(string commitMessage, Signature signature)
    {
        Table? firstTable = default;
        var fixture = new Fixture();
        var transformations = await Connection.UpdateAsync("main", CreateApplicationsAsync);
        var commit = await transformations.CommitAsync(new(commitMessage, signature, signature));

        async Task CreateApplicationsAsync(IChangeComposer composer)
        {
            for (int position = 1; position <= ApplicationCount; position++)
            {
                var application = await composer.CreateOrUpdateAsync(new Application
                {
                    Description = fixture.Create<string>(),
                    Name = fixture.Create<string>(),
                });
                await CreateTablesAsync(application, composer);
            }
        }

        async Task CreateTablesAsync(Application application, IChangeComposer composer)
        {
            for (int position = 1; position <= TablePerApplicationCount; position++)
            {
                var table = await composer.CreateOrUpdateAsync(new Table
                {
                    Description = fixture.Create<string>(),
                    Name = fixture.Create<string>(),
                }, parent: application);
                firstTable ??= table;
                await CreateFieldsAsync(table, composer);
                await CreateConstants(table, composer);
                await CreateResource(table, composer);
            }
        }

        async Task CreateFieldsAsync(Table table, IChangeComposer composer)
        {
            for (int position = 1; position <= FieldPerTableCount; position++)
            {
                var field = await composer.CreateOrUpdateAsync(new Field
                {
                    A = fixture.Create<NestedA[]>(),
                    SomeValue = fixture.Create<NestedA>(),
                    LinkedTable = firstTable,
                }, parent: table);
            }
        }

        async Task CreateConstants(Table table, IChangeComposer composer)
        {
            for (int position = 1; position <= ConstantPerTableCount; position++)
            {
                await composer.CreateOrUpdateAsync(new Constant
                {
                    Value = fixture.Create<string>(),
                }, parent: table);
            }
        }

        async Task CreateResource(Table table, IChangeComposer composer)
        {
            for (int position = 1; position <= ResourcePerTableCount; position++)
            {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(fixture.Create<string>()));
                var resource = new Resource(table,
                                            $"Path{UniqueId.CreateNew()}",
                                            $"File{UniqueId.CreateNew()}.txt",
                                            new Resource.Data(stream));
                await composer.CreateOrUpdateAsync(resource);
            }
        }

        fixture.Register(PickFirstApplication);
        fixture.Register(PickRandomTable);
        fixture.Register(PickRandomField);
        fixture.Register(PickRandomResource);

        return commit;

        Application PickFirstApplication() => fixture.Create<IConnection>().GetApplications(commit).Last();
        Table PickRandomTable() => fixture.Create<IConnection>().GetTables(commit, application: PickFirstApplication()).Last();
        Field PickRandomField() => fixture.Create<IConnection>().GetFields(commit, PickRandomTable()).Last();
        Resource PickRandomResource() => fixture.Create<IConnection>().GetResourcesAsync(commit, PickRandomTable()).ToEnumerable().OrderBy(f => f.Path).Last();
    }
}
