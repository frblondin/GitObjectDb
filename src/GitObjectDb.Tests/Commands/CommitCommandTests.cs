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
using Models.Software;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests.Commands;

[Parallelizable(ParallelScope.Self | ParallelScope.Children)]
public class CommitCommandTests
{
    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void AddNewNodeUsingNodeFolders(CommitCommandType commitType, IFixture fixture, Application application, UniqueId newTableId, string message, Signature signature)
    {
        // Arrange
        var updateCommand = fixture.Create<UpdateTreeCommand>();
        var updateFastInsert = fixture.Create<UpdateFastInsertFile>();
        var comparer = fixture.Create<Comparer>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(updateCommand, updateFastInsert, commitCommandFactory: sut, connection: connection);
        composer.CreateOrUpdate(new Table { Id = newTableId }, application);
        sut.Invoke(commitType).Commit(connection, composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{application.Path.FolderPath}/Pages/{newTableId}/{newTableId}.json";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void AddNewNodeWithoutNodeFolders(CommitCommandType commitType, IFixture fixture, Table table, UniqueId newFieldId, string message, Signature signature)
    {
        // Arrange
        var updateCommand = fixture.Create<UpdateTreeCommand>();
        var updateFastInsert = fixture.Create<UpdateFastInsertFile>();
        var comparer = fixture.Create<Comparer>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(updateCommand, updateFastInsert, commitCommandFactory: sut, connection: connection);
        composer.CreateOrUpdate(new Field { Id = newFieldId }, table);
        sut.Invoke(commitType).Commit(connection, composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{table.Path.FolderPath}/Fields/{newFieldId}.json";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void AddNewResource(CommitCommandType commitType, IFixture fixture, Table table, string fileContent, string message, Signature signature)
    {
        // Arrange
        var updateCommand = fixture.Create<UpdateTreeCommand>();
        var updateFastInsert = fixture.Create<UpdateFastInsertFile>();
        var comparer = fixture.Create<Comparer>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();
        var resource = new Resource(table, "Some/Folder", "File.txt", new Resource.Data(fileContent));

        // Act
        var composer = new TransformationComposer(updateCommand, updateFastInsert, commitCommandFactory: sut, connection: connection);
        composer.CreateOrUpdate(resource);
        sut.Invoke(commitType).Commit(connection, composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        var expectedPath = $"{table.Path.FolderPath}/{FileSystemStorage.ResourceFolder}/Some/Folder/File.txt";
        Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
        var loaded = (Resource)changes.Added.Single().New;
        Assert.That(loaded.Embedded.ReadAsString(), Is.EqualTo(fileContent));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void DeletingNodeRemovesNestedChildren(CommitCommandType commitType, IFixture fixture, Table table, string message, Signature signature)
    {
        // Arrange
        var updateCommand = fixture.Create<UpdateTreeCommand>();
        var updateFastInsert = fixture.Create<UpdateFastInsertFile>();
        var comparer = fixture.Create<Comparer>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        var composer = new TransformationComposer(updateCommand, updateFastInsert, commitCommandFactory: sut, connection: connection);
        composer.Delete(table);
        sut.Invoke(commitType).Commit(connection, composer, new(message, signature, signature));

        // Assert
        var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.GreaterThan(1));
    }

    [Test]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(
        new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks) },
        CommitCommandType.FastImport)]
    public void EditNestedProperty(CommitCommandType commitType, IFixture fixture, Field field, string message, Signature signature)
    {
        // Arrange
        var updateCommand = fixture.Create<UpdateTreeCommand>();
        var updateFastInsert = fixture.Create<UpdateFastInsertFile>();
        var comparer = fixture.Create<Comparer>();
        var sut = fixture.Create<ServiceResolver<CommitCommandType, ICommitCommand>>();
        var connection = fixture.Create<IConnectionInternal>();

        // Act
        field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
        var composer = new TransformationComposer(updateCommand, updateFastInsert, commitCommandFactory: sut, connection: connection);
        composer.CreateOrUpdate(field);
        sut.Invoke(commitType).Commit(connection, composer, new(message, signature, signature));

        // Act
        var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, connection.Model.DefaultComparisonPolicy);
        Assert.That(changes, Has.Count.EqualTo(1));
        Assert.That(changes.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
        Assert.That(changes.Added, Is.Empty);
        Assert.That(changes.Deleted, Is.Empty);
    }

    private class InternalMocks : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var connection = A.Fake<IConnectionInternal>(x => x.Strict());
            A.CallTo(() => connection.Repository).Returns(fixture.Create<IRepository>());
            A.CallTo(() => connection.Model).Returns(fixture.Create<IDataModel>());
            fixture.Inject(connection);

            var validation = A.Fake<ITreeValidation>(x => x.Strict());
            A.CallTo(validation).WithVoidReturnType().DoesNothing();
            fixture.Inject(validation);

            fixture.Inject(new CommitCommand(validation));
        }
    }
}
