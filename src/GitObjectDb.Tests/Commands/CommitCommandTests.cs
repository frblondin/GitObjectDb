using AutoFixture;
using FakeItEasy;
using GitObjectDb.Comparison;
using GitObjectDb.Internal;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace GitObjectDb.Tests.Commands
{
    [Parallelizable(ParallelScope.Self | ParallelScope.Children)]
    public class CommitCommandTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void AddNewNodeUsingNodeFolders(IFixture fixture, Application application, UniqueId newTableId, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);

            // Act
            var composer = new TransformationComposer(updateCommand, sut, connection);
            composer.CreateOrUpdate(new Table { Id = newTableId }, application);
            var result = sut.Commit(
                connection.Repository,
                composer.Transformations,
                message, signature, signature);

            // Assert
            var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.EqualTo(1));
            var expectedPath = $"{application.Path.FolderPath}/Pages/{newTableId}/{newTableId}.json";
            Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void AddNewNodeWithoutNodeFolders(IFixture fixture, Table table, UniqueId newFieldId, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);

            // Act
            var composer = new TransformationComposer(updateCommand, sut, connection);
            composer.CreateOrUpdate(new Field { Id = newFieldId }, table);
            var result = sut.Commit(
                connection.Repository,
                composer.Transformations,
                message, signature, signature);

            // Assert
            var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.EqualTo(1));
            var expectedPath = $"{table.Path.FolderPath}/Fields/{newFieldId}.json";
            Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void AddNewResource(IFixture fixture, Table table, string fileContent, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);
            var resource = new Resource(table, "Some/Folder", "File.txt", fileContent);

            // Act
            var composer = new TransformationComposer(updateCommand, sut, connection);
            composer.CreateOrUpdate(resource);
            sut.Commit(
                connection.Repository,
                composer.Transformations,
                message, signature, signature);

            // Assert
            var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.EqualTo(1));
            var expectedPath = $"{table.Path.FolderPath}/{FileSystemStorage.ResourceFolder}/Some/Folder/File.txt";
            Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
            var loaded = (Resource)changes.Added.Single().New;
            var content = new StreamReader(loaded.GetContentStream()).ReadToEnd();
            Assert.That(content, Is.EqualTo(fileContent));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void DeletingNodeRemovesNestedChildren(IFixture fixture, Table table, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);

            // Act
            var composer = new TransformationComposer(updateCommand, sut, connection);
            composer.Delete(table);
            sut.Commit(
                connection.Repository,
                composer.Transformations,
                message, signature, signature);

            // Assert
            var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.GreaterThan(1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void EditNestedProperty(IFixture fixture, Field field, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);

            // Act
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            var composer = new TransformationComposer(updateCommand, sut, connection);
            composer.CreateOrUpdate(field);
            sut.Commit(
                connection.Repository,
                composer.Transformations,
                message, signature, signature);

            // Act
            var changes = comparer.Compare(connection, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
        }

        private static (UpdateTreeCommand UpdateTreeCommand, Comparer Comparer, CommitCommand CommitCommand, IConnectionInternal Connection) Arrange(IFixture fixture) =>
        (
            fixture.Create<UpdateTreeCommand>(),
            fixture.Create<Comparer>(),
            fixture.Create<CommitCommand>(),
            fixture.Create<IConnectionInternal>()
        );

        private class InternalMocks : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                var connection = A.Fake<IConnectionInternal>(x => x.Strict());
                A.CallTo(() => connection.Repository).Returns(fixture.Create<Repository>());
                A.CallTo(() => connection.Head).Returns(fixture.Create<Repository>().Head);
                fixture.Inject(connection);

                var validation = A.Fake<ITreeValidation>(x => x.Strict());
                A.CallTo(validation).WithVoidReturnType().DoesNothing();
                fixture.Inject(validation);

                fixture.Inject(new CommitCommand(validation));
            }
        }
    }
}
