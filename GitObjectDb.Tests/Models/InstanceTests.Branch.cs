using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Git.Backends;
using LibGit2Sharp;
using NUnit.Framework;
using PowerAssert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public partial class InstanceTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MergeTwoDifferentPropertiesChanged(IInstanceLoader loader, Instance sut, Page page, Signature signature, string message, Func<RepositoryDescription, IComputeTreeChanges> computeTreeChangesFactory)
        {
            // master:    A---C---D
            //             \     /
            // newBranch:   B----

            // Arrange
            var repositoryDescription = GetRepositoryDescription();
            sut.SaveInNewRepository(signature, message, _path, repositoryDescription); // A

            // Act
            sut.Branch("newBranch");
            var updateName = page.With(p => p.Name == "modified name");
            sut.Commit(updateName.Instance, signature, message); // B
            sut.Checkout("master");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = sut.Commit(updateDescription.Instance, signature, message); // C
            var loaded = loader.LoadFrom<Instance>(GetRepositoryDescription());
            var mergeCommit = loaded.Merge("newBranch").Apply(signature); // D

            // Assert
            var changes = computeTreeChangesFactory(GetRepositoryDescription())
                .Compare(typeof(Instance), commitC.Id, mergeCommit.Id);
            Assert.That(changes.Modified, Has.Count.EqualTo(1));
            Assert.That(changes.Modified[0].Old.Name, Is.EqualTo(page.Name));
            Assert.That(changes.Modified[0].New.Name, Is.EqualTo(updateName.Name));
        }
    }
}
