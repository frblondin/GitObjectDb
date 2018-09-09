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
    [DebuggerDisplay(DebuggerDisplay + ", IsRepositoryAttached = {RepositoryDescription != null}")]
    [DataContract]
    public abstract partial class AbstractObjectRepository : AbstractModel, IObjectRepository
    {
        /// <summary>
        /// The migration folder.
        /// </summary>
        internal const string MigrationFolder = "$Migrations";

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractObjectRepository"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="container">The repository container.</param>
        /// <param name="id">The repository identifier.</param>
        /// <param name="name">The repository name.</param>
        /// <param name="version">The repository version.</param>
        /// <param name="dependencies">The repository dependencies.</param>
        /// <param name="migrations">The repository migrations.</param>
        [JsonConstructor]
        protected AbstractObjectRepository(IServiceProvider serviceProvider, IObjectRepositoryContainer container, Guid id, string name, System.Version version, IImmutableList<RepositoryDependency> dependencies, ILazyChildren<IMigration> migrations)
            : base(serviceProvider, id, name)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            Container = container ?? throw new ArgumentNullException(nameof(container));
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
            Migrations = (migrations ?? throw new ArgumentNullException(nameof(migrations))).AttachToParent(this);

            RepositoryProvider = serviceProvider.GetRequiredService<IRepositoryProvider>();
        }

        /// <inheritdoc />
        public override IObjectRepositoryContainer Container { get; }

        /// <inheritdoc />
        [DataMember]
        public System.Version Version { get; }

        /// <inheritdoc />
        [DataMember]
        public IImmutableList<RepositoryDependency> Dependencies { get; }

        /// <inheritdoc />
        [PropertyName(MigrationFolder)]
        public ILazyChildren<IMigration> Migrations { get; }

        /// <inheritdoc />
        public ObjectId CommitId { get; private set; }

        /// <inheritdoc />
        public IRepositoryProvider RepositoryProvider { get; }

        /// <inheritdoc />
        public RepositoryDescription RepositoryDescription { get; private set; }

        /// <inheritdoc />
        public IMetadataObject TryGetFromGitPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var chunks = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            IMetadataObject result = this;
            for (int i = 0; i < chunks.Length - 1 && result != null; i++)
            {
                var propertyInfo = result.DataAccessor.ChildProperties.FirstOrDefault(
                    p => p.FolderName.Equals(chunks[i], StringComparison.OrdinalIgnoreCase));
                if (propertyInfo == null)
                {
                    return null;
                }

                i++;
                if (i >= chunks.Length)
                {
                    return null;
                }

                var children = propertyInfo.Accessor(result);
                var guid = Guid.Parse(chunks[i]);
                result = children.FirstOrDefault(c => c.Id == guid);
            }
            return result;
        }

        /// <inheritdoc />
        public IMetadataObject GetFromGitPath(string path) =>
            TryGetFromGitPath(path) ?? throw new NotFoundException($"The element with path '{path}' could not be found.");

        /// <summary>
        /// Sets the repository data.
        /// </summary>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit getter.</param>
        internal void SetRepositoryData(RepositoryDescription repositoryDescription, ObjectId commitId)
        {
            RepositoryDescription = repositoryDescription ?? throw new ArgumentNullException(nameof(repositoryDescription));
            CommitId = commitId ?? throw new ArgumentNullException(nameof(commitId));
        }
    }
}
