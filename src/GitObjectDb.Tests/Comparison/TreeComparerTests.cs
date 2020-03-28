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
        public void CompareFieldEdit(IFixture fixture)
        {
            // Arrange
            var (sut, repository, comparer, field, message, signature) = Arrange(fixture);
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            sut
                .Update(c => c.CreateOrUpdate(field))
                .Commit(message, signature, signature);

            // Act
            var comparison = comparer.Compare(
                sut,
                repository.Lookup<Commit>("HEAD~1").Tree,
                repository.Head.Tip.Tree,
                ComparisonPolicy.Default);

            // Assert
            Assert.That(comparison, Has.Count.EqualTo(1));
            Assert.That(comparison.Modified.OfType<Change.NodeChange>().Single().Differences, Has.Count.EqualTo(1));
            Assert.That(comparison.Added, Is.Empty);
            Assert.That(comparison.Deleted, Is.Empty);
        }

        private static (IConnectionInternal Connection, Repository Repository, Comparer Comparer, Field Field, string StringValue, Signature Signature) Arrange(IFixture fixture) =>
        (
            fixture.Create<IConnectionInternal>(),
            fixture.Create<Repository>(),
            fixture.Create<Comparer>(),
            fixture.Create<Field>(),
            fixture.Create<string>(),
            fixture.Create<Signature>()
        );
    }
}
