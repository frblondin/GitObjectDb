using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    public partial class AbstractObjectRepository
    {
        /// <summary>
        /// The repository provider.
        /// </summary>
        internal readonly IRepositoryProvider _repositoryProvider;

        /// <summary>
        /// The repository description.
        /// </summary>
        internal RepositoryDescription _repositoryDescription;

        readonly IObjectRepositoryLoader _repositoryLoader;
        readonly GitHooks _hooks;

        /// <inheritdoc />
        public ObjectId CommitId { get; private set; }

        /// <summary>
        /// Sets the repository data.
        /// </summary>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="commitId">The commit getter.</param>
        internal void SetRepositoryData(RepositoryDescription repositoryDescription, ObjectId commitId)
        {
            _repositoryDescription = repositoryDescription;
            CommitId = commitId;
        }

        /// <inheritdoc />
        public ObjectId SaveInNewRepository(Signature signature, string message, RepositoryDescription repositoryDescription, bool isBare = false)
        {
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (repositoryDescription == null)
            {
                throw new ArgumentNullException(nameof(repositoryDescription));
            }

            LibGit2Sharp.Repository.Init(repositoryDescription.Path, isBare);

            return _repositoryProvider.Execute(repositoryDescription, repository =>
            {
                var all = this.Flatten().Select(o => new MetadataTreeEntryChanges(o.GetDataPath(), ChangeKind.Added, @new: o));
                var changes = new MetadataTreeChanges(this, all.ToImmutableList());
                var result = repository.CommitChanges(changes, message, signature, signature, hooks: _hooks);

                if (result != null)
                {
                    SetRepositoryData(repositoryDescription, result.Id);
                }

                return result?.Id;
            });
        }

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
    }
}
