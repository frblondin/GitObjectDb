using System;
using AutoFixture;
using FakeItEasy;
using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Models.Software;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Commands;

[Parallelizable(ParallelScope.Self | ParallelScope.Children)]
public class CommitCommandTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
    public void AddNewNodeUsingNodeFolders(IFixture fixture, Application application, UniqueId newTableId, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", gitUpdateCommand, sut);
        composer.CreateOrUpdate(new Table { Id = newTableId }, application);
        sut.Commit(composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{application.Path.FolderPath}/Pages/{newTableId}/{newTableId}.json";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
    public void AddNewNodeWithoutNodeFolders(IFixture fixture, Table table, UniqueId newFieldId, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", gitUpdateCommand, sut);
        composer.CreateOrUpdate(new Field { Id = newFieldId }, table);
        sut.Commit(composer, new(message, signature, signature));

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
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
    public void AddNewResource(IFixture fixture, Table table, string fileContent, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();
        var resource = new Resource(table, "Some/Folder", "File.txt", new Resource.Data(fileContent));

        // Act
        var composer = new TransformationComposer(connection, "main", gitUpdateCommand, sut);
        composer.CreateOrUpdate(resource);
        sut.Commit(composer, new(message, signature, signature));

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
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
    public void DeletingNodeRemovesNestedChildren(IFixture fixture, Table table, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", gitUpdateCommand, sut);
        composer.Delete(table);
        sut.Commit(composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.GreaterThan(1));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
    public void DeletingNodeRemovesExternallyStoredPropertyFiles(IFixture fixture, Constant constant, string message, Signature signature)
    {
        // Arrange
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", gitUpdateCommand, sut);
        composer.Delete(constant);
        sut.Commit(composer, new(message, signature, signature));

        // Assert
        var folder = connection.Repository.Branches["main"].Tip[constant.Path!.FolderPath].Target.Peel<Tree>();
        Assert.That(folder,
            Has.Exactly(0).Matches<TreeEntry>(entry =>
                entry.Name.StartsWith(constant.Id.ToString(), StringComparison.OrdinalIgnoreCase)));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
    public void RenamingNonGitFoldersIsSupported(IFixture fixture, Field field, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", gitUpdateCommand, sut);
        var newPath = new DataPath(field.Path.FolderPath,
                                   $"someName{Path.GetExtension(field.Path.FileName)}",
                                   field.Path.UseNodeFolders);
        composer.Rename(field, newPath);
        sut.Commit(composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection,
                                       connection.Repository.Lookup<Commit>("main~1"),
                                       connection.Repository.Head.Tip,
                                       connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
    public void RenamingGitFoldersIsNotSupported(IFixture fixture, Table table, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", gitUpdateCommand, sut);
        var newPath = new DataPath(table.Path.FolderPath,
                                   $"someName{Path.GetExtension(table.Path.FileName)}",
                                   table.Path.UseNodeFolders);
        Assert.Throws<GitObjectDbException>(() => composer.Rename(table, newPath));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
    public void EditNestedProperty(IFixture fixture, Field field, string message, Signature signature)
    {
        // Arrange
        var comparer = fixture.Create<Comparer>();
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(connection, "main", gitUpdateCommand, sut);
        composer.CreateOrUpdate(field with
        {
            SomeValue = new()
            {
                B = new()
                {
                    IsVisible = !field.SomeValue.B.IsVisible,
                },
            },
        });
        sut.Commit(composer, new(message, signature, signature));

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
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
    public void EditNodeRemovesObsoleteExternallyStoredPropertyFiles(IFixture fixture, Constant constant, string message, Signature signature)
    {
        // Arrange
        var gitUpdateCommand = fixture.Create<IGitUpdateCommand>();
        var sut = fixture.Create<ICommitCommand>();
        using var connection = fixture.Create<IConnectionInternal>();
        var definition = TreeDefinition.From(connection.Repository.Branches["main"].Tip);
        var obsoletePropertyPath = $"{constant.Path!.FolderPath}/{constant.Id}.ObsoletePropertyName.txt";
        definition.Add(
            obsoletePropertyPath,
            connection.Repository.ObjectDatabase.CreateBlob(
                new MemoryStream(Encoding.Default.GetBytes("Some content"))),
            Mode.NonExecutableFile);
        var tree = connection.Repository.ObjectDatabase.CreateTree(definition);
        var commit = connection.Repository.ObjectDatabase.CreateCommit(
            signature, signature, message,
            tree,
            new[] { connection.Repository.Branches["main"].Tip },
            false);
        connection.Repository.Refs.UpdateTarget(
            connection.Repository.Branches["main"].Reference, commit.Id, message);

        // Act
        var composer = new TransformationComposer(connection, "main", gitUpdateCommand, sut);
        composer.CreateOrUpdate(constant with { Value = constant.Value + "Updated" });
        sut.Commit(composer, new(message, signature, signature));

        // Assert
        Assert.That(connection.Repository.Branches["main"].Tip[obsoletePropertyPath], Is.Null);
    }

    private class InternalMocks : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Inject<IMemoryCache>(new MemoryCache(Options.Create(new MemoryCacheOptions())));

            var connection = A.Fake<IConnectionInternal>(x => x.Strict());
            A.CallTo(() => connection.Repository).Returns(fixture.Create<IRepository>());
            A.CallTo(() => connection.Model).Returns(fixture.Create<IDataModel>());
            A.CallTo(() => connection.Serializer).Returns(fixture.Create<INodeSerializer>());
            A.CallTo(() => connection.Cache).Returns(fixture.Create<IMemoryCache>());
            A.CallTo(() => connection.Dispose()).DoesNothing();
            fixture.Inject(connection);

            var validation = A.Fake<ITreeValidation>(x => x.Strict());
            A.CallTo(validation).WithVoidReturnType().DoesNothing();
            fixture.Inject(validation);
        }
    }
}
