using AutoFixture;
using FakeItEasy;
using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Internal;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Models.Software;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Change = GitObjectDb.Comparison.Change;

namespace GitObjectDb.Tests.Commands;

public class CommitCommandTests
{
    [Test]
    public async Task AddNewNodeUsingNodeFolders()
    {
        // Arrange
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var application = fixture.Create<Application>();
        var newTableId = fixture.Create<UniqueId>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        await composer.CreateOrUpdateAsync(new Table { Id = newTableId }, application);
        await sut.CommitAsync(composer, new(message, signature, signature));

        // Assert
        var changes = await comparer.CompareAsync(connection,
            await connection.Repository.GetCommittishAsync("main~1"),
            await connection.Repository.GetCommittishAsync("main"),
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{application.Path.FolderPath}/Pages/{newTableId}/{newTableId}.json";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
    }

    [Test]
    public async Task AddNewNodeWithoutNodeFolders()
    {
        // Arrange
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var table = fixture.Create<Table>();
        var newFieldId = fixture.Create<UniqueId>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        await composer.CreateOrUpdateAsync(new Field { Id = newFieldId }, table);
        await sut.CommitAsync(composer, new(message, signature, signature));

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
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var table = fixture.Create<Table>();
        var fileContent = fixture.Create<string>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();
        var resource = new Resource(table, "Some/Folder", "File.txt", new Resource.Data(fileContent));

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        await composer.CreateOrUpdateAsync(resource);
        await sut.CommitAsync(composer, new(message, signature, signature));

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
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var table = fixture.Create<Table>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        await composer.DeleteAsync(table);
        await sut.CommitAsync(composer, new(message, signature, signature));

        // Assert
        var changes = await comparer.CompareAsync(connection,
            await connection.Repository.GetCommittishAsync("main~1"),
            await connection.Repository.GetCommittishAsync("main"),
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.GreaterThan(1));
    }

    [Test]
    public async Task DeletingNodeRemovesExternallyStoredPropertyFiles()
    {
        // Arrange
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var constant = fixture.Create<Constant>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        await composer.DeleteAsync(constant);
        await sut.CommitAsync(composer, new(message, signature, signature));

        // Assert
        var tip = await connection.Repository.GetCommittishAsync("main");
        var tree = await tip.GetRootTreeAsync();
        var folderItem = await tree.GetFromPathAsync(constant.Path!.FolderPath);
        var folder = await folderItem.GetEntryAsync<TreeEntry>();
        Assert.That(folder.Children,
            Has.Exactly(0).Matches<TreeEntryItem>(entry =>
                entry.Name.StartsWith(constant.Id.ToString(), StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    public async Task RenamingNonGitFoldersIsSupported()
    {
        // Arrange
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var field = fixture.Create<Field>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        var newPath = new DataPath(field.Path.FolderPath,
                                   $"someName{Path.GetExtension(field.Path.FileName)}",
                                   field.Path.UseNodeFolders);
        await composer.RenameAsync(field, newPath);
        await sut.CommitAsync(composer, new(message, signature, signature));

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
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var table = fixture.Create<Table>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        var newPath = new DataPath(table.Path.FolderPath,
                                   $"someName{Path.GetExtension(table.Path.FileName)}",
                                   table.Path.UseNodeFolders);
        Assert.ThrowsAsync<GitObjectDbException>(() => composer.RenameAsync(table, newPath));
    }

    [Test]
    public async Task EditNestedProperty()
    {
        // Arrange
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var field = fixture.Create<Field>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        await composer.CreateOrUpdateAsync(field with
        {
            SomeValue = new()
            {
                B = new()
                {
                    IsVisible = !field.SomeValue.B.IsVisible,
                },
            },
        });
        await sut.CommitAsync(composer, new(message, signature, signature));

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
    public async Task EditNodeAddsExternallyStoredPropertyFiles()
    {
        // Arrange
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var table = fixture.Create<Table>();
        var value = fixture.Create<string>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        await composer.CreateOrUpdateAsync(table with { LongTextStoredInSeparateBlob = value });
        await sut.CommitAsync(composer, new(message, signature, signature));

        // Assert
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
            Assert.That(changes.Modified.OfType<Change.NodeChange>().Single().New,
                Is.InstanceOf<Table>().With.Property(nameof(Table.LongTextStoredInSeparateBlob)).EqualTo(value));
        });
    }

    [Test]
    public async Task EditNodeRemovesObsoleteExternallyStoredPropertyFiles()
    {
        // Arrange
        var fixture = (await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>()).Customize(new InternalMocks());
        var constant = fixture.Create<Constant>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();
        var obsoletePropertyPath = $"{constant.Path!.FolderPath}/{constant.Id}.ObsoletePropertyName.txt";
        var tip = await connection.Repository.GetCommittishAsync("main");
        await connection.Repository.CommitAsync("main",
            c => c.AddOrUpdate(obsoletePropertyPath, Encoding.Default.GetBytes("Some content")),
            connection.Repository.CreateCommit(message, [tip], signature, signature));

        // Act
        var composer = new ChangeComposer(connection, "main", gitUpdateCommand, sut);
        await composer.CreateOrUpdateAsync(constant with { Value = constant.Value + "Updated" });
        await sut.CommitAsync(composer, new(message, signature, signature));

        // Assert
        tip = await connection.Repository.GetCommittishAsync("main");
        var tree = await tip.GetRootTreeAsync();
        Assert.That(await tree.GetFromPathAsync(obsoletePropertyPath), Is.Null);
    }

    private class InternalMocks : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject<IMemoryCache>(new MemoryCache(Options.Create(new MemoryCacheOptions())));

            var internalConnection = fixture.Create<IGitConnection>();
            var connection = A.Fake<IConnectionInternal>(o => o
                .Strict()
                .ConfigureFake(f =>
                {
                    A.CallTo(() => f.Repository).Returns(internalConnection);
                    A.CallTo(() => f.Model).Returns(fixture.Create<IDataModel>());
                    A.CallTo(() => f.Serializer).Returns(fixture.Create<INodeSerializer>());
                    A.CallTo(() => f.Cache).Returns(fixture.Create<IMemoryCache>());
                    A.CallTo(() => f.Dispose()).Invokes(() => internalConnection.Dispose());
                }));
            fixture.Inject(connection);

            // No need to validate tree in tests
            var validation = A.Fake<ITreeValidation>();
            fixture.Inject<ICommitCommand>(new CommitCommand(() => validation));
        }
    }
}
