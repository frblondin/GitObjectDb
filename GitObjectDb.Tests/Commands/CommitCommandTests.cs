using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var connection = fixture.Create<IConnectionInternal>();
            var sut = fixture.Create<CommitCommand>();

            // Act
            var transformations = new NodeTransformationComposer(sut, connection)
                .Create(new Field(newFieldId), table)
                .Transformations;
            var result = sut.Commit(
                connection.Repository,
                transformations.Select(t => t.Transformation),
                message, signature, signature);

            // Assert
            var changes = TreeComparer.Compare(connection.Repository, connection.Repository.Lookup<Commit>("HEAD~1").Tree);
            Assert.That(changes, Has.Count.EqualTo(1));
            var expectedPath = $"{table.Path.FolderPath}/Fields/{newFieldId}/{FileSystemStorage.DataFile}";
            Assert.That(changes.Added.Single().New.Path.DataPath, Is.EqualTo(expectedPath));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void DeletingNodeRemovesNestedChildren(IFixture fixture, Table table, string message, Signature signature)
        {
            // Arrange
            var connection = fixture.Create<IConnectionInternal>();
            var sut = fixture.Create<CommitCommand>();

            // Act
            var transformations = new NodeTransformationComposer(sut, connection)
                .Delete(table)
                .Transformations;
            var result = sut.Commit(
                connection.Repository,
                transformations.Select(t => t.Transformation),
                message, signature, signature);

            // Assert
            var changes = TreeComparer.Compare(connection.Repository, connection.Repository.Lookup<Commit>("HEAD~1").Tree);
            Assert.That(changes, Has.Count.GreaterThan(1));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization), typeof(InternalMocks))]
        public void EditNestedProperty(IFixture fixture, Field field, string message, Signature signature)
        {
            // Arrange
            var connection = fixture.Create<IConnectionInternal>();
            var sut = fixture.Create<CommitCommand>();

            // Act
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            var transformations = new NodeTransformationComposer(sut, connection)
                .Update(field)
                .Transformations;
            var result = sut.Commit(
                connection.Repository,
                transformations.Select(t => t.Transformation),
                message, signature, signature);

            // Act
            var changes = TreeComparer.Compare(connection.Repository, connection.Repository.Lookup<Commit>("HEAD~1").Tree);
            Assert.That(changes, Has.Count.EqualTo(1));
            Assert.That(changes.Modified.Single().Differences, Has.Count.EqualTo(1));
            Assert.That(changes.Added, Is.Empty);
            Assert.That(changes.Deleted, Is.Empty);
        }

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
