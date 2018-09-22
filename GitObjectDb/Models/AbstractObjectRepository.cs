using GitObjectDb.Attributes;
using GitObjectDb.Git;
using GitObjectDb.Models.Migration;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
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
        [Modifiable]
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
        public IMetadataObject TryGetFromGitPath(ObjectPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var repository = Id == path.Repository ?
                this :
                Container.TryGetRepository(path.Repository);
            return repository?.TryGetFromGitPath(path.Path);
        }

        /// <inheritdoc />
        public IMetadataObject GetFromGitPath(ObjectPath path) =>
            TryGetFromGitPath(path) ?? throw new NotFoundException($"The element with path '{path}' could not be found.");

        /// <inheritdoc />
        public IMetadataObject TryGetFromGitPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Equals(FileSystemStorage.DataFile, StringComparison.OrdinalIgnoreCase))
            {
                return this;
            }

            var chunks = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (chunks.Length < 2)
            {
                return null;
            }

            IMetadataObject result = this;
            for (int i = 0; result != null && i < chunks.Length - 1; i += 2)
            {
                var propertyInfo = result.DataAccessor.ChildProperties.TryGetWithValue(
                    p => p.FolderName,
                    chunks[i]);
                if (propertyInfo == null)
                {
                    return null;
                }

                var children = propertyInfo.Accessor(result);
                result = Guid.TryParse(chunks[i + 1], out var guid) ?
                    children.FirstOrDefault(c => c.Id == guid) :
                    null;
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
