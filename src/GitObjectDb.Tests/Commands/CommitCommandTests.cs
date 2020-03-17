using AutoFixture;
using FakeItEasy;
using GitObjectDb.Commands;
using GitObjectDb.Comparison;
using GitObjectDb.Internal;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Models.Software;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Validations;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Commands
{
    [Parallelizable(ParallelScope.All)]
    public class CommitCommandTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void AddNewField(IFixture fixture, Table table, UniqueId newFieldId, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);

            // Act
            var transformations = new NodeTransformationComposer(updateCommand, sut, connection)
                .CreateOrUpdate(new Field(newFieldId), table)
                .Transformations;
            var result = sut.Commit(
                connection.Repository,
                transformations.Select(t => t.Transformation),
                message, signature, signature);

            // Assert
            var changes = comparer.Compare(connection.Repository, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.EqualTo(1));
            var expectedPath = $"{table.Path.FolderPath}/Fields/{newFieldId}/{FileSystemStorage.DataFile}";
            Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void AddNewResource(IFixture fixture, Table table, string fileContent, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);
            var relativePath = new DataPath("Some/Folder", "File.txt");
            var resource = table.Resources.Add(relativePath, Encoding.Default.GetBytes(fileContent));

            // Act
            var transformations = new NodeTransformationComposer(updateCommand, sut, connection)
                .CreateOrUpdate(resource)
                .Transformations;
            var result = sut.Commit(
                connection.Repository,
                transformations.Select(t => t.Transformation),
                message, signature, signature);

            // Assert
            var changes = comparer.Compare(connection.Repository, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.EqualTo(1));
            var expectedPath = $"{table.Path.FolderPath}/{FileSystemStorage.ResourceFolder}/{relativePath.FilePath}";
            Assert.That(changes.Added.Single().New.Path.FilePath, Is.EqualTo(expectedPath));
            var loaded = (Resource)changes.Added.Single().New;
            var content = new StreamReader(loaded.GetContentStream()).ReadToEnd();
            Assert.That(content, Is.EqualTo(fileContent));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void DeletingNodeRemovesNestedChildren(IFixture fixture, Table table, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);

            // Act
            var transformations = new NodeTransformationComposer(updateCommand, sut, connection)
                .Delete(table)
                .Transformations;
            var result = sut.Commit(
                connection.Repository,
                transformations.Select(t => t.Transformation),
                message, signature, signature);

            // Assert
            var changes = comparer.Compare(connection.Repository, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.GreaterThan(1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void EditResource(IFixture fixture, Table table, string fileContent, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);
            var resource = table.Resources.First();
            resource.SetContentStream(
                new MemoryStream(Encoding.Default.GetBytes(fileContent)));

            // Act
            var transformations = new NodeTransformationComposer(updateCommand, sut, connection)
                .CreateOrUpdate(resource)
                .Transformations;
            var result = sut.Commit(
                connection.Repository,
                transformations.Select(t => t.Transformation),
                message, signature, signature);

            // Assert
            var changes = comparer.Compare(connection.Repository, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.EqualTo(1));
            var loaded = (Resource)changes.Modified.Single().New;
            var content = new StreamReader(loaded.GetContentStream()).ReadToEnd();
            Assert.That(content, Is.EqualTo(fileContent));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void EditNestedProperty(IFixture fixture, Field field, string message, Signature signature)
        {
            // Arrange
            var (updateCommand, comparer, sut, connection) = Arrange(fixture);

            // Act
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            var transformations = new NodeTransformationComposer(updateCommand, sut, connection)
                .CreateOrUpdate(field)
                .Transformations;
            var result = sut.Commit(
                connection.Repository,
                transformations.Select(t => t.Transformation),
                message, signature, signature);

            // Act
            var changes = comparer.Compare(connection.Repository, connection.Repository.Lookup<Commit>("HEAD~1").Tree, connection.Repository.Head.Tip.Tree, ComparisonPolicy.Default);
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
        }

        private static (UpdateTreeCommand, Comparer, CommitCommand, IConnectionInternal) Arrange(IFixture fixture) =>
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
                fixture.Inject(connection);

                var validation = A.Fake<ITreeValidation>(x => x.Strict());
                A.CallTo(validation).WithVoidReturnType().DoesNothing();
                fixture.Inject(validation);

                fixture.Inject(new CommitCommand(validation));
            }
        }
    }
}
