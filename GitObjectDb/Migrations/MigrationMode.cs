using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Migrations
{
    /// <summary>
    /// Specifies the migration mode.
    /// </summary>
    public enum MigrationMode
    {
        /// <summary>
        /// Upgrade mode.
        /// </summary>
        Upgrade,

        /// <summary>
        /// Downgrade mode.
        /// </summary>
        Downgrade
    }
}
