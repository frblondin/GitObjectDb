using AutoFixture;
using GitObjectDb.Comparison;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace GitObjectDb.Tests.Commands;

[Parallelizable(ParallelScope.Self | ParallelScope.Children)]
public class IndexCommandTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void AddNewNodeUsingNodeFolders(IFixture fixture, Application application, UniqueId newTableId, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = connection.GetIndex("main",  c => c.CreateOrUpdate(new Table { Id = newTableId }, application));
        index.Commit(new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
            connection.Repository.Lookup<Commit>("main~1"),
            connection.Repository.Head.Tip,
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
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void AddNewNodeWithoutNodeFolders(IFixture fixture, Table table, UniqueId newFieldId, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = connection.GetIndex("main", c => c.CreateOrUpdate(new Field { Id = newFieldId }, table));
        index.Commit(new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{table.Path.FolderPath}/Fields/{newFieldId}.json";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void AddNewResource(IFixture fixture, Table table, string fileContent, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();
        var resource = new Resource(table, "Some/Folder", "File.txt", new Resource.Data(fileContent));

        // Act
        var index = connection.GetIndex("main", c => c.CreateOrUpdate(resource));
        index.Commit(new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{table.Path.FolderPath}/{FileSystemStorage.ResourceFolder}/Some/Folder/File.txt";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
        var loaded = (Resource)changes.Added.Single().New;
        Assert.That(loaded.Embedded.ReadAsString(), Is.EqualTo(fileContent));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void DeletingNodeRemovesNestedChildren(IFixture fixture, Table table, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = connection.GetIndex("main", c => c.Delete(table));
        index.Commit(new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.GreaterThan(1));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void RenamingNonGitFoldersIsSupported(IFixture fixture, Field field, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var newPath = new DataPath(field.Path.FolderPath,
                                   $"someName{Path.GetExtension(field.Path.FileName)}",
                                   field.Path.UseNodeFolders);
        var index = connection.GetIndex("main", c => c.Rename(field, newPath));
        index.Commit(new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void RenamingGitFoldersIsNotSupported(IFixture fixture, Table table, string message, Signature signature)
    {
        // Arrange
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var newPath = new DataPath(table.Path.FolderPath,
                                   $"someName{Path.GetExtension(table.Path.FileName)}",
                                   table.Path.UseNodeFolders);
        Assert.Throws<GitObjectDbException>(() =>
        {
            var index = connection.GetIndex("main", c => c.Rename(table, newPath));
            index.Commit(new(message, signature, signature));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void EditNestedProperty(IFixture fixture, Field field, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = connection.GetIndex("main", c => c.CreateOrUpdate(field with
        {
            SomeValue = new()
            {
                B = new()
                {
                    IsVisible = !field.SomeValue.B.IsVisible,
                },
            },
        }));
        index.Commit(new(message, signature, signature));

        // Act
        var changes = comparer.Compare(connection,
            connection.Repository.Lookup<Commit>("main~1"),
            connection.Repository.Head.Tip,
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(changes.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
            Assert.That(new FileInfo(((Internal.Index)index).IndexStoragePath),
                        Has.Property(nameof(FileInfo.Exists)).False);
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void EditPropertyStoredAsSeparateFile(IFixture fixture, Constant constant, string value, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = connection.GetIndex("main", c => c.CreateOrUpdate(constant with
        {
            Value = value,
        }));
        index.Commit(new(message, signature, signature));

        // Act
        var changes = comparer.Compare(connection,
            connection.Repository.Lookup<Commit>("main~1"),
            connection.Repository.Head.Tip,
            connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(changes.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
            Assert.That(new FileInfo(((Internal.Index)index).IndexStoragePath),
                        Has.Property(nameof(FileInfo.Exists)).False);
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CommitIndexAfterBranchTipHasChangedThrowsAnException(IFixture fixture, Application application, UniqueId newTableId, string description, string message, Signature signature)
    {
        // Arrange
        using var connection = fixture.Create<IConnectionInternal>();
        var tip = connection.Repository.Branches["main"].Tip;

        // Act
        var index = connection.GetIndex("main", c => c.CreateOrUpdate(new Table { Id = newTableId }, application));
        connection.Update("main", c => c.CreateOrUpdate(application with { Description = description }))
            .Commit(new(message, signature, signature));

        // Assert
        Assert.That(index.CommitId, Is.EqualTo(tip.Id));
        Assert.Throws<GitObjectDbException>(() => index.Commit(new(message, signature, signature)));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void UpdateIndexAfterBranchTipHasChangedThrowsAnException(IFixture fixture, Application application, UniqueId newTableId, string description, string message, Signature signature)
    {
        // Arrange
        using var connection = fixture.Create<IConnectionInternal>();
        var tip = connection.Repository.Branches["main"].Tip;

        // Act
        var index = connection.GetIndex("main", c => c.CreateOrUpdate(new Table { Id = newTableId }, application));
        connection.Update("main", c => c.CreateOrUpdate(application with { Description = description }))
            .Commit(new(message, signature, signature));

        // Assert
        Assert.That(index.CommitId, Is.EqualTo(tip.Id));
        Assert.Throws<GitObjectDbException>(() => index.CreateOrUpdate(application with { Description = string.Empty }));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void UpdateIndexAfterBranchTipHasChangedCanTargetNewTip(IFixture fixture, Application application, UniqueId newTableId, string description, string message, Signature signature)
    {
        // Arrange
        using var connection = fixture.Create<IConnectionInternal>();
        var tip = connection.Repository.Branches["main"].Tip;
        var index = connection.GetIndex("main", c => c.CreateOrUpdate(new Table { Id = newTableId }, application));
        connection.Update("main", c => c.CreateOrUpdate(application with { Description = description }))
            .Commit(new(message, signature, signature));

        // Act
        index.UpdateToBranchTip();

        // Assert
        Assert.That(index.CommitId, Is.EqualTo(connection.Repository.Branches["main"].Tip.Id));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void UpdateIndexIfNotYetModifiedDoesNotThrowAnException(IFixture fixture, Application application, string description, string message, Signature signature)
    {
        // Arrange
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var index = connection.GetIndex("main");
        var indexVersion = index.Version;
        connection.Update("main", c => c.CreateOrUpdate(application with { Description = description }))
            .Commit(new(message, signature, signature));

        // Assert
        Assert.That(index.CommitId, Is.Null);
        index.CreateOrUpdate(application with { Description = string.Empty });
        Assert.That(index.Version, Is.Not.EqualTo(indexVersion));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void ResetIndexIsClearingAnyStagedChange(IFixture fixture, Application application)
    {
        // Arrange
        using var connection = fixture.Create<IConnectionInternal>();
        var index = connection.GetIndex("main",
                                        c => c.CreateOrUpdate(application with { Description = string.Empty }));
        var indexVersion = index.Version;

        // Act
        index.Reset();
        var newlyFetchedIndex = connection.GetIndex("main");

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
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void RevertChange(IFixture fixture, Application application)
    {
        // Arrange
        using var connection = fixture.Create<IConnectionInternal>();
        var index = connection.GetIndex("main");
        application = index.CreateOrUpdate(application with { Description = string.Empty });
        var indexVersion = index.Version;

        // Act
        index.Revert(application.Path);
        var newlyFetchedIndex = connection.GetIndex("main");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(index, Is.Empty);
            Assert.That(newlyFetchedIndex, Is.Empty);
            Assert.That(index.Version, Is.Not.EqualTo(indexVersion));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void IndexCount(IFixture fixture, Application application)
    {
        // Arrange
        using var connection = fixture.Create<IConnectionInternal>();
        var index = connection.GetIndex("main");

        // Act
        index.CreateOrUpdate(application with { Description = string.Empty });

        // Assert
        Assert.That(index, Has.Count.EqualTo(1));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void EnumerateEntries(IFixture fixture, Application application, UniqueId newTableId)
    {
        // Arrange
        using var connection = fixture.Create<IConnectionInternal>();
        var tip = connection.Repository.Branches["main"].Tip;
        var index = connection.GetIndex("main", c => c.CreateOrUpdate(new Table { Id = newTableId }, application));

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
