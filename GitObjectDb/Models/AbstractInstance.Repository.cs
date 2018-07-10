using GitObjectDb.Git;
using GitObjectDb.Reflection;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    public partial class AbstractInstance
    {
        readonly StringBuilder _jsonBuffer = new StringBuilder();

        /// <summary>
        /// The repository provider.
        /// </summary>
        internal readonly IRepositoryProvider _repositoryProvider;

        /// <summary>
        /// The repository description.
        /// </summary>
        internal RepositoryDescription _repositoryDescription;

        readonly IInstanceLoader _instanceLoader;

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
        public Commit SaveInNewRepository(Signature signature, string message, string path, RepositoryDescription repositoryDescription, bool isBare = false)
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

            Repository.Init(path, isBare);

            return _repositoryProvider.Execute(repositoryDescription, repository =>
            {
                var result = repository.Commit(AddMetadataObjectToCommit, message, signature, signature);
                SetRepositoryData(repositoryDescription, result.Id);
                return result;
            });
        }

        void AddMetadataObjectToCommit(IRepository repository, TreeDefinition tree) =>
            AddNodeToCommit(repository, tree, new Stack<string>(), this);

        void AddNodeToCommit(IRepository repository, TreeDefinition tree, Stack<string> stack, IMetadataObject node)
        {
            var path = stack.ToDataPath();
            node.ToJson(_jsonBuffer);
            tree.Add(path, repository.CreateBlob(_jsonBuffer), Mode.NonExecutableFile);
            AddNodeChildrenToCommit(repository, tree, stack, node);
        }

        void AddNodeChildrenToCommit(IRepository repository, TreeDefinition tree, Stack<string> stack, IMetadataObject node)
        {
            var dataAccessor = DataAccessorProvider.Get(node.GetType());
            foreach (var childProperty in dataAccessor.ChildProperties)
            {
                var children = childProperty.Accessor(node);
                stack.Push(childProperty.Name);
                foreach (var child in children)
                {
                    stack.Push(child.Id.ToString());
                    AddNodeToCommit(repository, tree, stack, child);
                    stack.Pop();
                }
                stack.Pop();
            }
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
                var dataAccessor = DataAccessorProvider.Get(result.GetType());
                var propertyInfo = dataAccessor.ChildProperties.FirstOrDefault(
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
    }
}
