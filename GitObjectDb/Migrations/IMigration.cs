using GitObjectDb.Models;
using System;

namespace GitObjectDb.Migrations
{
    /// <summary>
    /// Represents the interface for migrations.
    /// </summary>
    public interface IMigration : IMetadataObject
    {
        /// <summary>
        /// Gets a value indicating whether this instance supports downgrade.
        /// </summary>
        bool CanDowngrade { get; }

        /// <summary>
        /// Gets a value indicating whether this instance can be used at any version.
        /// </summary>
        bool IsIdempotent { get; }

        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        void Up();

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        void Down();
    }
}