using AutoFixture;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Compare;
using GitObjectDb.Services;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Models.Migration;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Migrations;
using LibGit2Sharp;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PowerAssert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public partial class ObjectRepositoryTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void RebaseTwoDifferentPropertiesChanged(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, Identity committer)
        {
            // master:    A---B
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updateName = page.With(p => p.Name == "modified name");
            var b = container.Commit(updateName.Repository, signature, message); // B
            container.Branch(a.Id, "newBranch", "HEAD~1");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = container.Commit(updateDescription.Repository, signature, message); // C
            var rebase = commitC.Rebase.Start("master", committer);

            // Assert
            Assert.That(rebase.Status, Is.EqualTo(RebaseStatus.Complete));
            Assert.That(rebase.TotalStepCount, Is.EqualTo(1));
            var (commits, tip) = container.Repositories.Single().Execute(r =>
            {
                var commitFilter = new CommitFilter
                {
                    IncludeReachableFrom = r.Head.Tip,
                    SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
                };
                return (r.Commits.QueryBy(commitFilter).Select(c => c.Id).ToList(), r.Head.Tip.Id);
            });
            Assert.That(commits[0], Is.EqualTo(a.CommitId));
            Assert.That(commits[1], Is.EqualTo(b.CommitId));
            Assert.That(commits[2], Is.EqualTo(tip));
            Assert.That(container.Repositories.Single().CommitId, Is.EqualTo(tip));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void RebaseFailsWhenSourceBranchContainsMigrations(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, DummyMigration migration, Identity committer)
        {
            // master:    A---B  (B contains a migration)
            //             \
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updatedInstance = sut.With(i => i.Migrations.Add(migration));
            container.Commit(updatedInstance.Repository, signature, message); // B
            container.Branch(a.Id, "newBranch", "HEAD~1");
            var updateDescription = page.With(p => p.Description == "modified description");
            var commitC = container.Commit(updateDescription.Repository, signature, message); // C
            Assert.Throws<NotSupportedException>(() => commitC.Rebase.Start("master", committer));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void RebaseFailsWhenBranchContainsMigrations(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Page page, Signature signature, string message, DummyMigration migration, Identity committer)
        {
            // master:    A---B
            //             \
            // newBranch:   C  (C contains a migration)   ->   A---B---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updateName = page.With(p => p.Name == "modified name");
            container.Commit(updateName.Repository, signature, message); // B
            container.Branch(a.Id, "newBranch", "HEAD~1");
            var updatedInstance = sut.With(i => i.Migrations.Add(migration));
            var commitC = container.Commit(updatedInstance.Repository, signature, message); // C
            Assert.Throws<NotSupportedException>(() => commitC.Rebase.Start("master", committer));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void RebaseFailsWhenConflicts(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, Identity committer)
        {
            // master:    A---B
            //             \    (B & C change same value)
            // newBranch:   C   ->   A---B---C

            // Arrange
            var a = container.AddRepository(sut, signature, message); // A

            // Act
            var updateName = a.Applications[0].Pages[0].With(p => p.Name == "foo");
            container.Commit(updateName.Repository, signature, message); // B
            container.Branch(a.Id, "newBranch", "HEAD~1");
            var updateDescription = a.Applications[0].Pages[0].With(p => p.Name == "bar");
            var commitC = container.Commit(updateDescription.Repository, signature, message); // C
            Assert.Throws<NotSupportedException>(() => commitC.Rebase.Start("master", committer));
        }
    }
}
