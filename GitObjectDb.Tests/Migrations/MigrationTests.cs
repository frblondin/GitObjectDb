using AutoFixture;
using GitObjectDb.Git;
using GitObjectDb.Migrations;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Migrations
{
    public class MigrationTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void MigrationScaffolderDetectsRequiredChanges(IFixture fixture, IServiceProvider serviceProvider, ObjectRepository sut, Signature signature, string message)
        {
            // Arrange
            var repositoryDescription = RepositoryFixture.GetRepositoryDescription();
            sut.SaveInNewRepository(signature, message, repositoryDescription);
            var updated = sut.With(i => i.Migrations.Add(fixture.Create<Migration>()));
            var commit = sut.Commit(updated, signature, message);

            // Act
            var migrationScaffolder = new MigrationScaffolder(serviceProvider, repositoryDescription);
            var migrators = migrationScaffolder.Scaffold(sut.CommitId, commit, MigrationMode.Upgrade);

            // Assert
            Assert.That(migrators, Has.Count.EqualTo(1));
            Assert.That(migrators[0].CommitId, Is.EqualTo(commit));
            Assert.That(migrators[0].Mode, Is.EqualTo(MigrationMode.Upgrade));
            Assert.That(migrators[0].Migrations, Has.Count.EqualTo(1));
        }
    }
}
