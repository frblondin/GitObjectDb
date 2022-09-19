using AutoFixture;
using GitObjectDb.Internal;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;

namespace GitObjectDb.Tests;

public class TreeValidationTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CannotCommitParentDeletionAndChildAddition(IFixture fixture, IConnection connection, Application application, Table table)
    {
        // Act
        var composer = (TransformationComposer)connection
            .Update(c =>
            {
                c.Delete(application);
                c.CreateOrUpdate(new Field { }, table);
            });
        var tree = UpdateTree(connection, composer);

        // Assert
        var sut = fixture.Create<TreeValidation>();
        Assert.Throws<GitObjectDbValidationException>(() => sut.Validate(tree, connection.Model, connection.Serializer));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CannotCommitChildWithInvalidPath(IFixture fixture, IConnection connection)
    {
        // Act
        var composer = (TransformationComposer)connection
            .Update(c => c.CreateOrUpdate(new Field { Path = new DataPath("InvalidFolder", "invalidfile.json", true) }));
        var tree = UpdateTree(connection, composer);

        // Assert
        var sut = fixture.Create<TreeValidation>();
        Assert.Throws<GitObjectDbValidationException>(() => sut.Validate(tree, connection.Model, connection.Serializer));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CannotCommitChildWithNoParent(IFixture fixture, IConnection connection, Application application, Field field, string message, Signature signature)
    {
        // Arrange, delete parent
        connection
            .Update(c => c.Delete(application))
            .Commit(new(message, signature, signature));

        // Act, edit child
        var composer = (TransformationComposer)connection
            .Update(c => c.CreateOrUpdate(field));
        var tree = UpdateTree(connection, composer);

        // Assert
        var sut = fixture.Create<TreeValidation>();
        Assert.Throws<GitObjectDbValidationException>(() => sut.Validate(tree, connection.Model, connection.Serializer));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CannotCommitTwoNodesWithSameId(IFixture fixture, IConnection connection, Field field)
    {
        // Act
        var composer = (TransformationComposer)connection
            .Update(c => c.CreateOrUpdate(new Application { Id = field.Id }));
        var tree = UpdateTree(connection, composer);

        // Assert
        var sut = fixture.Create<TreeValidation>();
        Assert.Throws<GitObjectDbValidationException>(() => sut.Validate(tree, connection.Model, connection.Serializer));
    }

    private static Tree UpdateTree(IConnection connection, TransformationComposer composer)
    {
        var repository = ((IConnectionInternal)connection).Repository;
        var definition = composer.ApplyTransformations(repository.ObjectDatabase, connection.Repository.Head.Tip);
        var tree = repository.ObjectDatabase.CreateTree(definition);
        return tree;
    }
}
