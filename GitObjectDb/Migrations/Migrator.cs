using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitObjectDb.Migrations
{
    /// <summary>
    /// Migrator is used to apply existing migrations to a database. Migrator can be used to upgrade and downgrade
    /// to any given migration. To scaffold migrations based on changes to your model use MigrationScaffolder
    /// </summary>
    public class Migrator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Migrator"/> class.
        /// </summary>
        /// <param name="migrations">The migrations.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="commitId">The commit identifier containing the migrations.</param>
        /// <exception cref="ArgumentNullException">migrations</exception>
        public Migrator(IImmutableList<IMigration> migrations, MigrationMode mode, ObjectId commitId)
        {
            Migrations = migrations ?? throw new ArgumentNullException(nameof(migrations));
            Mode = mode;
            CommitId = commitId ?? throw new ArgumentNullException(nameof(commitId));
        }

        /// <summary>
        /// Gets the migrations used by this migrator.
        /// </summary>
        public IImmutableList<IMigration> Migrations { get; }

        /// <summary>
        /// Gets the migration mode.
        /// </summary>
        public MigrationMode Mode { get; }

        /// <summary>
        /// Gets the commit identifier containing the migrations.
        /// </summary>
        public ObjectId CommitId { get; }

        /// <summary>
        /// Applies the migrations.
        /// </summary>
        public void Apply()
        {
            if (Mode == MigrationMode.Upgrade)
            {
                foreach (var migration in Migrations)
                {
                    migration.Up();
                }
            }
            else
            {
                ThrowIfAnyDowngradeNotSupported();

                foreach (var migration in Migrations.Reverse())
                {
                    migration.Down();
                }
            }
        }

        void ThrowIfAnyDowngradeNotSupported()
        {
            if (Migrations.Any(m => !m.CanDowngrade))
            {
                throw new NotSupportedException("One or more migrations do not support downgrading.");
            }
        }
    }
}
