using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitObjectDb.Comparison;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Models.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;

namespace GitObjectDb.Tests.Comparison
{
    public class TreeComparerTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(SoftwareCustomization))]
        public void CompareFieldEdit(IConnection sut, Repository repository, Field field, string message, Signature author, Signature committer)
        {
            // Arrange
            field.A[0].B.IsVisible = !field.A[0].B.IsVisible;
            sut
                .Update(c => c.Update(field))
                .Commit(message, author, committer);

            // Act
            var comparison = TreeComparer.Compare(repository, repository.Lookup<Commit>("HEAD~1").Tree);

            // Assert
            Assert.That(comparison, Has.Count.EqualTo(1));
            Assert.That(comparison.Modified.Single().Differences, Has.Count.EqualTo(1));
            Assert.That(comparison.Added, Is.Empty);
            Assert.That(comparison.Deleted, Is.Empty);
        }
    }
}
