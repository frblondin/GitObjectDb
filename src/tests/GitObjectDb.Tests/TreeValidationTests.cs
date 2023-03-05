using AutoFixture;
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
    public void CannotCommitParentDeletionAndChildAddition(IFixture fixture, IConnection connection, Application application, Table table, string message, Signature signature)
    {
        // Act, Assert
        Assert.Throws<GitObjectDbValidationException>(() => connection
            .Update("main", c =>
            {
                c.Delete(application);
                c.CreateOrUpdate(new Field { }, table);
            })
            .Commit(new(message, signature, signature)));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CannotCommitChildWithInvalidPath(IConnection connection, string message, Signature signature)
    {
        // Act, Assert
        Assert.Throws<GitObjectDbValidationException>(() => connection
            .Update("main",
                    c => c.CreateOrUpdate(new Field { Path = new DataPath("InvalidFolder", "invalidfile.json", true) }))
            .Commit(new(message, signature, signature)));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CannotCommitChildWithNoParent(IConnection connection, Application application, Field field, string message, Signature signature)
    {
        // Arrange, delete parent
        connection
            .Update("main", c => c.Delete(application))
            .Commit(new(message, signature, signature));

        // Act, edit child
        Assert.Throws<GitObjectDbValidationException>(() => connection
            .Update("main", c => c.CreateOrUpdate(field))
            .Commit(new(message, signature, signature)));
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CanCommitTwoNodesWithSameId(IConnection connection, Field field, string message, Signature signature)
    {
        // Act, Assert
        connection
            .Update("main", c => c.CreateOrUpdate(new Application { Id = field.Id }))
            .Commit(new(message, signature, signature));
    }
}
