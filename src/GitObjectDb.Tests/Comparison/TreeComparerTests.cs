using AutoFixture;
using GitObjectDb.Comparison;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests.Comparison
{
    public class TreeComparerTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void CompareFieldEdit(IConnection connection, Repository repository, Field field, string message, Signature signature)
        {
            // Arrange
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            connection
                .Update(c => c.CreateOrUpdate(field))
                .Commit(message, signature, signature);

            // Act
            var comparison = connection.Compare("HEAD~1", repository.Head.Tip.Sha);

            // Assert
            Assert.That(comparison, Has.Count.EqualTo(1));
            Assert.That(comparison.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(comparison.Added, Is.Empty);
            Assert.That(comparison.Deleted, Is.Empty);
        }
    }
}
