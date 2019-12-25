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
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Migrations
{
    public class MigratorTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task MigrationScaffolderDetectsRequiredChangesAsync(ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, IFixture fixture, IServiceProvider serviceProvider, Signature signature, string message)
        {
            // Arrange
            repository = await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);
            var updated = repository.With(c => c.Add(repository, r => r.Migrations, fixture.Create<DummyMigration>()));
            var commit = await container.CommitAsync(updated, signature, message).ConfigureAwait(false);

            // Act
            var migrationScaffolder = new MigrationScaffolder(container, repository.RepositoryDescription,
                serviceProvider.GetRequiredService<IRepositoryProvider>(), serviceProvider.GetRequiredService<ObjectRepositorySerializerFactory>());
            var migrators = await migrationScaffolder.ScaffoldAsync(repository.CommitId, commit.CommitId, MigrationMode.Upgrade).ConfigureAwait(false);

            // Assert
            Assert.That(migrators, Has.Count.EqualTo(1));
            Assert.That(migrators[0].CommitId, Is.EqualTo(commit.CommitId));
            Assert.That(migrators[0].Mode, Is.EqualTo(MigrationMode.Upgrade));
            Assert.That(migrators[0].Migrations, Has.Count.EqualTo(1));
        }
    }
}
