using GitObjectDb.Comparison;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Models.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;
using System.Linq;

namespace GitObjectDb.Tests
{
    public class ConnectionTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void SameHeadAsUnderlyingRepository(IConnection sut, Repository repository)
        {
            // Assert
            Assert.That(sut.Head.Tip, Is.EqualTo(repository.Head.Tip));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void SameBranchCountAsUnderlyingRepository(IConnection sut, Repository repository)
        {
            // Assert
            Assert.That(sut.Branches.Count(), Is.EqualTo(repository.Branches.Count()));
        }
    }
}