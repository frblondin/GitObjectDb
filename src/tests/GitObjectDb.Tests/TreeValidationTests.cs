using AutoFixture;
using GitDotNet;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;
using System.Threading.Tasks;

namespace GitObjectDb.Tests;

public class TreeValidationTests
{
    [Test]
    public async Task CannotCommitParentDeletionAndChildAddition()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var connection = fixture.Create<IConnection>();
        var application = fixture.Create<Application>();
        var table = fixture.Create<Table>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        // Delete parent
        var removeParentChange = await connection.UpdateAsync("main", async c =>
        {
            await c.DeleteAsync(application);
        });
        await removeParentChange.CommitAsync(new(message, signature, signature));

        // Act, Assert
        Assert.ThrowsAsync<GitObjectDbValidationException>(async () =>
        {
            var changes = await connection.UpdateAsync("main", async c =>
            {
                await c.CreateOrUpdateAsync(table);
            });
            await changes.CommitAsync(new(message, signature, signature));
        });
    }

    [Test]
    public async Task CannotCommitChildWithInvalidPath()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var connection = fixture.Create<IConnection>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        // Act, Assert
        Assert.ThrowsAsync<GitObjectDbValidationException>(async () =>
        {
            var changes = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(new Field { Path = new DataPath("InvalidFolder", "invalidfile.json", true) }));
            await changes.CommitAsync(new(message, signature, signature));
        });
    }

    [Test]
    public async Task CannotCommitChildWithNoParent()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var connection = fixture.Create<IConnection>();
        var application = fixture.Create<Application>();
        var field = fixture.Create<Field>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        // Delete parent
        var changes = await connection.UpdateAsync("main", c => c.DeleteAsync(application));
        await changes.CommitAsync(new(message, signature, signature));

        // Act, edit child
        Assert.ThrowsAsync<GitObjectDbValidationException>(async () =>
        {
            var changes = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(field));
            await changes.CommitAsync(new(message, signature, signature));
        });
    }

    [Test]
    public async Task CanCommitTwoNodesWithSameId()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var connection = fixture.Create<IConnection>();
        var field = fixture.Create<Field>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        // Act, Assert
        var changes = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(new Application { Id = field.Id }));
        await changes.CommitAsync(new(message, signature, signature));
    }
}
