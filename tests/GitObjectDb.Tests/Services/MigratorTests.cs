using AutoFixture;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Migration;
using GitObjectDb.Services;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Models.Migration;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using GitObjectDb.Serialization;

namespace GitObjectDb.Tests.Migrations
{
    public class MigratorTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void MigrationScaffolderDetectsRequiredChanges(ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, IFixture fixture, IServiceProvider serviceProvider, Signature signature, string message)
        {
            // Arrange
            repository = container.AddRepository(repository, signature, message);
            var updated = repository.With(c => c.Add(repository, r => r.Migrations, fixture.Create<DummyMigration>()));
            var commit = container.Commit(updated, signature, message);

            // Act
            var migrationScaffolder = new MigrationScaffolder(container, repository.RepositoryDescription,
                serviceProvider.GetRequiredService<IRepositoryProvider>(), serviceProvider.GetRequiredService<ObjectRepositorySerializerFactory>());
            var migrators = migrationScaffolder.Scaffold(repository.CommitId, commit.CommitId, MigrationMode.Upgrade);

            // Assert
            Assert.That(migrators, Has.Count.EqualTo(1));
            Assert.That(migrators[0].CommitId, Is.EqualTo(commit.CommitId));
            Assert.That(migrators[0].Mode, Is.EqualTo(MigrationMode.Upgrade));
            Assert.That(migrators[0].Migrations, Has.Count.EqualTo(1));
        }
    }
}
