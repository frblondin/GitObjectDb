using AutoFixture;
using GitDotNet;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Change = GitObjectDb.Comparison.Change;

namespace GitObjectDb.Tests.Comparison;

public class TreeComparerTests
{
    [Test]
    public async Task CompareFieldEdit()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var connection = fixture.Create<IConnection>();
        var field = fixture.Create<Field>();
        var message = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var changes = await connection
            .UpdateAsync("main", c => c.CreateOrUpdateAsync(
                field with
                {
                    SomeValue = new NestedA
                    {
                        B = new NestedB { IsVisible = !field.SomeValue.B.IsVisible },
                    },
                }));
        await changes.CommitAsync(new(message, signature, signature));

        // Act
        var comparison = await connection.CompareAsync("main~1", "main");

        // Assert
        Assert.That(comparison, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(comparison.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(comparison.Added, Is.Empty);
            Assert.That(comparison.Deleted, Is.Empty);
        });
    }
}
