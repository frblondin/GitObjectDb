using AutoFixture;
using GitObjectDb.Internal;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests;

public class TreeValidationTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CannotCommitParentDeletionAndChildAddition(IFixture fixture, IConnection connection, Application application, Table table)
    {
        // Act
        var composer = (TransformationComposer)connection
            .Update("main", c =>
            {
                c.Delete(application);
                c.CreateOrUpdate(new Field { }, table);
            }, CommitCommandType.Normal);
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
            .Update("main",
                    c => c.CreateOrUpdate(new Field { Path = new DataPath("InvalidFolder", "invalidfile.json", true) }),
                    CommitCommandType.Normal);
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
            .Update("main", c => c.Delete(application), CommitCommandType.Normal)
            .Commit(new(message, signature, signature));

        // Act, edit child
        var composer = (TransformationComposer)connection
            .Update("main", c => c.CreateOrUpdate(field), CommitCommandType.Normal);
        var tree = UpdateTree(connection, composer);

        // Assert
        var sut = fixture.Create<TreeValidation>();
        Assert.Throws<GitObjectDbValidationException>(() => sut.Validate(tree, connection.Model, connection.Serializer));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CanCommitTwoNodesWithSameId(IFixture fixture, IConnection connection, Field field)
    {
        // Act
        var composer = (TransformationComposer)connection
            .Update("main", c => c.CreateOrUpdate(new Application { Id = field.Id }), CommitCommandType.Normal);
        var tree = UpdateTree(connection, composer);

        // Assert
        var sut = fixture.Create<TreeValidation>();
        sut.Validate(tree, connection.Model, connection.Serializer);
    }

    private static Tree UpdateTree(IConnection connection, TransformationComposer composer)
    {
        var repository = connection.Repository;
        var commit = repository.Head.Tip;
        var definition = TreeDefinition.From(commit);
        var modules = new ModuleCommands(commit?.Tree);
        foreach (var transformation in composer.Transformations.Cast<ITransformationInternal>())
        {
            var action = (ApplyUpdateTreeDefinition)transformation.Action;
            action.Invoke(commit?.Tree, modules, connection.Serializer, connection.Repository.ObjectDatabase, definition);
        }
        return repository.ObjectDatabase.CreateTree(definition);
    }
}
