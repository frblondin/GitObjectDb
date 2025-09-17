using AutoFixture;
using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Change = GitObjectDb.Comparison.Change;

namespace GitObjectDb.Tests.Commands;

public class IndexCommandTests
{
    [Test]
    public async Task AddNewNodeUsingNodeFolders()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var application = fixture.Create<Application>();
        var newTableId = fixture.Create<UniqueId>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = await connection.GetIndexAsync("main",  c => c.CreateOrUpdateAsync(new Table { Id = newTableId }, application));
        await index.CommitAsync(new(message, signature, signature));

        // Assert
        var changes = await comparer.CompareAsync(connection,
            await connection.Repository.GetCommittishAsync("main~1"),
            await connection.Repository.GetCommittishAsync("main"),
            connection.Model.DefaultComparisonPolicy);
        var expectedPath = $"{application.Path.FolderPath}/Pages/{newTableId}/{newTableId}.json";
        Assert.Multiple(() =>
        {
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
            Assert.That(index.CommitId, Is.Null);
        });
    }

    [Test]
    public async Task AddNewNodeWithoutNodeFolders()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var table = fixture.Create<Table>();
        var newFieldId = fixture.Create<UniqueId>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(new Field { Id = newFieldId }, table));
        await index.CommitAsync(new(message, signature, signature));

        // Assert
        var changes = await comparer.CompareAsync(connection,
            await connection.Repository.GetCommittishAsync("main~1"),
            await connection.Repository.GetCommittishAsync("main"),
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{table.Path.FolderPath}/Fields/{newFieldId}.json";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
    }

    [Test]
    public async Task AddNewResource()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var table = fixture.Create<Table>();
        var fileContent = fixture.Create<string>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();
        var resource = new Resource(table, "Some/Folder", "File.txt", new Resource.Data(fileContent));

        // Act
        var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(resource));
        await index.CommitAsync(new(message, signature, signature));

        // Assert
        var changes = await comparer.CompareAsync(connection,
            await connection.Repository.GetCommittishAsync("main~1"),
            await connection.Repository.GetCommittishAsync("main"),
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{table.Path.FolderPath}/{FileSystemStorage.ResourceFolder}/Some/Folder/File.txt";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
        var loaded = (Resource)changes.Added.Single().New;
        Assert.That(await loaded.Embedded.ReadAsStringAsync(), Is.EqualTo(fileContent));
    }

    [Test]
    public async Task DeletingNodeRemovesNestedChildren()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var table = fixture.Create<Table>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = await connection.GetIndexAsync("main", c => c.DeleteAsync(table));
        await index.CommitAsync(new(message, signature, signature));

        // Assert
        var changes = await comparer.CompareAsync(connection,
            await connection.Repository.GetCommittishAsync("main~1"),
            await connection.Repository.GetCommittishAsync("main"),
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.GreaterThan(1));
    }

    [Test]
    public async Task RenamingNonGitFoldersIsSupported()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var field = fixture.Create<Field>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var newPath = new DataPath(field.Path.FolderPath,
                                   $"someName{Path.GetExtension(field.Path.FileName)}",
                                   field.Path.UseNodeFolders);
        var index = await connection.GetIndexAsync("main", c => c.RenameAsync(field, newPath));
        await index.CommitAsync(new(message, signature, signature));

        // Assert
        var changes = await comparer.CompareAsync(connection,
            await connection.Repository.GetCommittishAsync("main~1"),
            await connection.Repository.GetCommittishAsync("main"),
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task RenamingGitFoldersIsNotSupported()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var table = fixture.Create<Table>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var newPath = new DataPath(table.Path.FolderPath,
            $"someName{Path.GetExtension(table.Path.FileName)}",
            table.Path.UseNodeFolders);
        Assert.ThrowsAsync<GitObjectDbException>(async () =>
        {
            var index = await connection.GetIndexAsync("main", c => c.RenameAsync(table, newPath));
            await index.CommitAsync(new(message, signature, signature));
        });
    }

    [Test]
    public async Task EditNestedProperty()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var field = fixture.Create<Field>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(field with
        {
            SomeValue = new()
            {
                B = new()
                {
                    IsVisible = !field.SomeValue.B.IsVisible,
                },
            },
        }));
        await index.CommitAsync(new(message, signature, signature));

        // Act
        var changes = await comparer.CompareAsync(connection,
            await connection.Repository.GetCommittishAsync("main~1"),
            await connection.Repository.GetCommittishAsync("main"),
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(changes.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
        });
    }

    [Test]
    public async Task EditPropertyStoredAsSeparateFile()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var constant = fixture.Create<Constant>();
        var value = fixture.Create<string>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(constant with
        {
            Value = value,
        }));
        await index.CommitAsync(new(message, signature, signature));

        // Act
        var changes = await comparer.CompareAsync(connection,
            await connection.Repository.GetCommittishAsync("main~1"),
            await connection.Repository.GetCommittishAsync("main"),
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(changes.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
        });
    }

    [Test]
    public async Task CommitIndexAfterBranchTipHasChangedThrowsAnException()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var application = fixture.Create<Application>();
        var newTableId = fixture.Create<UniqueId>();
        var description = fixture.Create<string>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        using var connection = fixture.Create<IConnectionInternal>();
        var tip = connection.Repository.Branches["main"].Tip;

        // Act
        var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(new Table { Id = newTableId }, application));
        var changes = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(application with { Description = description }));
        await changes.CommitAsync(new(message, signature, signature));

        // Assert
        Assert.That(index.CommitId, Is.EqualTo(tip));
        Assert.ThrowsAsync<GitObjectDbException>(async () => await index.CommitAsync(new(message, signature, signature)));
    }

    [Test]
    public async Task UpdateIndexAfterBranchTipHasChangedThrowsAnException()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var application = fixture.Create<Application>();
        var newTableId = fixture.Create<UniqueId>();
        var description = fixture.Create<string>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        using var connection = fixture.Create<IConnectionInternal>();
        var tip = connection.Repository.Branches["main"].Tip;

        // Act
        var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(new Table { Id = newTableId }, application));
        var changes = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(application with { Description = description }));
        await changes.CommitAsync(new(message, signature, signature));

        // Assert
        Assert.That(index.CommitId, Is.EqualTo(tip));
        Assert.ThrowsAsync<GitObjectDbException>(() => index.CreateOrUpdateAsync(application with { Description = string.Empty }));
    }

    [Test]
    public async Task UpdateIndexAfterBranchTipHasChangedCanTargetNewTip()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var application = fixture.Create<Application>();
        var newTableId = fixture.Create<UniqueId>();
        var description = fixture.Create<string>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        using var connection = fixture.Create<IConnectionInternal>();
        var tip = connection.Repository.Branches["main"].Tip;
        var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(new Table { Id = newTableId }, application));
        var changes = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(application with { Description = description }));
        await changes.CommitAsync(new(message, signature, signature));

        // Act
        await index.UpdateToBranchTipAsync();

        // Assert
        Assert.That(index.CommitId, Is.EqualTo(connection.Repository.Branches["main"].Tip));
    }

    [Test]
    public async Task UpdateIndexIfNotYetModifiedDoesNotThrowAnException()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var application = fixture.Create<Application>();
        var description = fixture.Create<string>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = await connection.GetIndexAsync("main");
        var indexVersion = index.Version;
        var changes = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(application with { Description = description }));
        await changes.CommitAsync(new(message, signature, signature));

        // Assert
        Assert.That(index.CommitId, Is.Null);
        await index.CreateOrUpdateAsync(application with { Description = string.Empty });
        Assert.That(index.Version, Is.Not.EqualTo(indexVersion));
    }

    [Test]
    public async Task ResetIndexIsClearingAnyStagedChange()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var application = fixture.Create<Application>();

        using var connection = fixture.Create<IConnectionInternal>();
        var index = await connection.GetIndexAsync("main",
            c => c.CreateOrUpdateAsync(application with { Description = string.Empty }));
        var indexVersion = index.Version;

        // Act
        index.Reset();
        var newlyFetchedIndex = await connection.GetIndexAsync("main");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(index.CommitId, Is.Null);
            Assert.That(index, Is.Empty);
            Assert.That(newlyFetchedIndex.CommitId, Is.Null);
            Assert.That(newlyFetchedIndex, Is.Empty);
            Assert.That(index.Version, Is.Not.EqualTo(indexVersion));
        });
    }

    [Test]
    public async Task RevertChange()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var application = fixture.Create<Application>();

        using var connection = fixture.Create<IConnectionInternal>();
        var index = await connection.GetIndexAsync("main");
        application = await index.CreateOrUpdateAsync(application with { Description = string.Empty });
        var indexVersion = index.Version;

        // Act
        await index.RevertAsync(application.Path);
        var newlyFetchedIndex = await connection.GetIndexAsync("main");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(index, Is.Empty);
            Assert.That(newlyFetchedIndex, Is.Empty);
            Assert.That(index.Version, Is.Not.EqualTo(indexVersion));
        });
    }

    [Test]
    public async Task IndexCount()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var application = fixture.Create<Application>();

        using var connection = fixture.Create<IConnectionInternal>();
        var index = await connection.GetIndexAsync("main");

        // Act
        await index.CreateOrUpdateAsync(application with { Description = string.Empty });

        // Assert
        Assert.That(index, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task EnumerateEntries()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var application = fixture.Create<Application>();
        var newTableId = fixture.Create<UniqueId>();

        using var connection = fixture.Create<IConnectionInternal>();
        var tip = connection.Repository.Branches["main"].Tip;
        var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(new Table { Id = newTableId }, application));

        // Act
        foreach (var entry in index)
        {
            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(entry.IsValid, Is.True);
                Assert.That(entry.IsFrozen, Is.True);
                Assert.That(entry.Path, Is.Not.Null);
                Assert.That(entry.Data, Is.Not.Null);
                Assert.That(entry.Delete, Is.False);
            });
        }
    }
}
