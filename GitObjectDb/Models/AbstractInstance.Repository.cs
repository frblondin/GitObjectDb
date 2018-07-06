using GitObjectDb.Git;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    public partial class AbstractInstance : IInstance
    {
        readonly StringBuilder _jsonBuffer = new StringBuilder();

        /// <summary>
        /// The repository provider.
        /// </summary>
        internal readonly IRepositoryProvider _repositoryProvider;

        /// <summary>
        /// Holds a <see cref="Tree"/> provider from a <see cref="Repository"/>.
        /// </summary>
        internal Func<IRepository, Tree> _getTree;

        /// <summary>
        /// The repository description.
        /// </summary>
        internal RepositoryDescription _repositoryDescription;

        /// <summary>
        /// Sets the repository data.
        /// </summary>
        /// <param name="repositoryDescription">The repository description.</param>
        /// <param name="getTree">The tree getter.</param>
        internal void SetRepositoryData(RepositoryDescription repositoryDescription, Func<IRepository, Tree> getTree)
        {
            _repositoryDescription = repositoryDescription;
            _getTree = getTree;
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
                SetRepositoryData(repositoryDescription, r => r.Head.Tip.Tree);
                return result;
            });
        }

        /// <inheritdoc />
        public Commit Commit(AbstractInstance newInstance, Signature signature, string message, CommitOptions options = null)
        {
            if (newInstance == null)
            {
                throw new ArgumentNullException(nameof(newInstance));
            }
            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature));
            }
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                var computeChanges = _computeTreeChangesFactory(_repositoryDescription);
                var changes = computeChanges.Compare(this, newInstance, repository);
                return changes.AnyChange ?
                    repository.Commit(changes.NewTree, message, signature, signature, options) :
                    null;
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
                stack.Push(childProperty.Property.Name);
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
            for (int i = 0; i < chunks.Length && result != null; i++)
            {
                var propertyInfo = DataAccessorProvider.Get(result.GetType()).ChildProperties.FirstOrDefault(p =>
                    p.Property.Name.Equals(chunks[i], StringComparison.OrdinalIgnoreCase));
                if (propertyInfo == null || ++i >= chunks.Length)
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
