using GitObjectDb.Attributes;
using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Migrations;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Abstract root model containing nested <see cref="IMetadataObject"/> children.
    /// </summary>
    /// <seealso cref="AbstractModel" />
    /// <seealso cref="IObjectRepository" />
    [DebuggerDisplay(DebuggerDisplay + ", IsRepositoryAttached = {_repositoryDescription != null}")]
    [DataContract]
    public abstract partial class AbstractObjectRepository : AbstractModel, IObjectRepository
    {
        /// <summary>
        /// The migration folder.
        /// </summary>
        internal const string MigrationFolder = "$Migrations";

        readonly Func<RepositoryDescription, IComputeTreeChanges> _computeTreeChangesFactory;
        readonly Func<RepositoryDescription, IObjectRepository, string, IMetadataTreeMerge> _metadataTreeMergeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractObjectRepository"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="migrations">The migrations.</param>
        [JsonConstructor]
        protected AbstractObjectRepository(IServiceProvider serviceProvider, Guid id, string name, ILazyChildren<IMigration> migrations)
            : base(serviceProvider, id, name)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _computeTreeChangesFactory = serviceProvider.GetRequiredService<Func<RepositoryDescription, IComputeTreeChanges>>();
            _metadataTreeMergeFactory = serviceProvider.GetRequiredService<Func<RepositoryDescription, IObjectRepository, string, IMetadataTreeMerge>>();
            _repositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();
            _repositoryLoader = serviceProvider.GetRequiredService<IObjectRepositoryLoader>();
            _hooks = serviceProvider.GetRequiredService<GitHooks>();
            Migrations = (migrations ?? throw new ArgumentNullException(nameof(migrations))).AttachToParent(this);
        }

        /// <summary>
        /// Gets the migrations.
        /// </summary>
        [PropertyName(MigrationFolder)]
        public ILazyChildren<IMigration> Migrations { get; }
    }
}
