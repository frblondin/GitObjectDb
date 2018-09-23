using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Models.Migration;
using LibGit2Sharp;
using System.Collections.Immutable;

namespace GitObjectDb.Services
{
    /// <summary>
    /// Creates a new instance implementing the <see cref="IMigrationScaffolder"/> interface.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="repositoryDescription">The repository description.</param>
    /// <returns>The newly created instance.</returns>
    public delegate IMigrationScaffolder MigrationScaffolderFactory(IObjectRepositoryContainer container, RepositoryDescription repositoryDescription);

    /// <summary>
    /// Scaffolds migrations to apply pending model changes.
    /// </summary>
    public interface IMigrationScaffolder
    {
        /// <summary>
        /// Scaffolds a code based migration to apply any pending model changes to the database.
        /// </summary>
        /// <param name="migrationStart">The start.</param>
        /// <param name="migrationEnd">The end.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>The <see cref="Migrator"/> used to apply migrations.</returns>
        IImmutableList<Migrator> Scaffold(ObjectId migrationStart, ObjectId migrationEnd, MigrationMode mode);
    }
}