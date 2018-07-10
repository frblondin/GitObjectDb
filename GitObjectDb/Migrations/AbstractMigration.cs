using GitObjectDb.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Migrations
{
    /// <summary>
    /// Represents the base class for migrations.
    /// </summary>
    public abstract class AbstractMigration : AbstractModel, IMigration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractMigration"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        protected AbstractMigration(IServiceProvider serviceProvider, Guid id, string name)
            : base(serviceProvider, id, name)
        {
        }

        /// <inheritdoc/>
        public abstract bool CanDowngrade { get; }

        /// <inheritdoc/>
        public abstract bool IsIdempotent { get; }

        /// <inheritdoc/>
        public abstract void Up();

        /// <inheritdoc/>
        public abstract void Down();
    }
}
