using AutoFixture;
using GitDotNet;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Queries;

public class QueryNodesTests
{
    [Test]
    public async Task RootNodes()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.GetNodesAsync<Application>(tip).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(SoftwareBenchmarkCustomization.DefaultApplicationCount));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Path, Is.Not.Null);
            Assert.That(result[0].Name, Is.Not.Null);
            Assert.That(result[0].Description, Is.Not.Null);
        });
    }

    [Test]
    public async Task TablesInApplication()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();
        var application = fixture.Create<Application>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.GetNodesAsync<Table>(tip, parent: application).ToEnumerable().ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Path, Is.Not.Null);
            Assert.That(result[0].Name, Is.Not.Null);
            Assert.That(result[0].Description, Is.Not.Null);
        });
    }

    [Test]
    public async Task OfType()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = (from f in connection.GetNodesAsync<Field>(tip, isRecursive: true).ToEnumerable()
                      select f.Id).ToList();

        // Assert
        var expected = SoftwareBenchmarkCustomization.DefaultApplicationCount *
            SoftwareBenchmarkCustomization.DefaultTablePerApplicationCount *
            SoftwareBenchmarkCustomization.DefaultFieldPerTableCount;
        Assert.That(result, Has.Count.EqualTo(expected));
    }

    [Test]
    public async Task StoreAsSeparateFilePropertiesGetsLoaded()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var constant = fixture.Create<Constant>();

        // Assert
        Assert.That(constant.Value, Is.Not.Null);
    }

    [Test]
    public async Task LookupByPass()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();
        var table = fixture.Create<Table>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = await connection.LookupAsync<Table>(tip, table.Path);

        // Assert
        Assert.That(result, Is.EqualTo(table));
    }

    [Test]
    public async Task LookupById()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareBenchmarkCustomization>();
        var connection = fixture.Create<IConnection>();
        var table = fixture.Create<Table>();

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var result = await connection.LookupAsync<Table>(tip, table.Path);

        // Assert
        Assert.That(result, Is.EqualTo(table));
    }

    [Test]
    public async Task ModifiedReferenceEditedInIndexGetsResolved()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var connection = fixture.Create<IConnection>();
        var field = fixture.Create<Field>();
        var newDescription = fixture.Create<string>();

        var index = await connection.GetIndexAsync("main",
            c => c.CreateOrUpdateAsync(field.LinkedTable with { Description = newDescription }));

        // Act
        var resolvedField = await index.TryLoadItemAsync<Field>(field.Path);

        // Assert
        Assert.That(resolvedField.LinkedTable.Description, Is.EqualTo(newDescription));
    }

    [Test]
    public async Task TryLoadItemGetsItemsFromIndex()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var connection = fixture.Create<IConnection>();
        var field = fixture.Create<Field>();
        var newDescription = fixture.Create<string>();

        var index = await connection.GetIndexAsync("main",
            c => c.CreateOrUpdateAsync(field.LinkedTable with { Description = newDescription }));

        // Act
        var resolvedTable = await index.TryLoadItemAsync<Table>(field.LinkedTable.Path);

        // Assert
        Assert.That(resolvedTable.Description, Is.EqualTo(newDescription));
    }

    [Test]
    public async Task GetNodeHistory()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var connection = fixture.Create<IConnection>();
        var field = fixture.Create<Field>();
        var newDescription = fixture.Create<string>();
        var commitDescription = fixture.Create<CommitDescription>();

        var index = await connection.GetIndexAsync("main",
            c => c.CreateOrUpdateAsync(field with { Description = newDescription }));
        await index.CommitAsync(commitDescription);

        // Act
        var tip = await connection.Repository.GetCommittishAsync("main");
        var commits = await connection.GetLogsAsync(tip, field)
            .SelectAwait(async entry => await entry.GetCommitAsync())
            .ToListAsync();

        // Assert
        Assert.That(commits, Has.Count.EqualTo(2));
        Assert.That(commits, Has.One.Items.Matches<CommitEntry>(
            c => c.Message.Equals(commitDescription.Message, System.StringComparison.Ordinal)));
    }
}