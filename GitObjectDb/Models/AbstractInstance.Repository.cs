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
        /// Holds a <see cref="Tree"/> provider from a <see cref="Repository"/>.
        /// </summary>
        internal Func<Repository, Tree> _getTree;
        Func<Repository> _getRepository;

        /// <summary>
        /// Sets the repository data.
        /// </summary>
        /// <param name="getRepository">The repository getter.</param>
        /// <param name="getTree">The tree getter.</param>
        internal void SetRepositoryData(Func<Repository> getRepository, Func<Repository, Tree> getTree)
        {
            _getRepository = getRepository;
            _getTree = getTree;
        }

        /// <summary>
        /// Gets the repository that must be disposed by the caller.
        /// </summary>
        /// <returns>A new <see cref="Repository"/> instance.</returns>
        /// <exception cref="NullReferenceException">The module is not attached to a repository.</exception>
        internal Repository GetRepository() =>
            _getRepository?.Invoke() ?? throw new NotSupportedException("The module is not attached to a repository.");

        /// <inheritdoc />
        public Commit SaveInNewRepository(Signature signature, string message, string path, Func<Repository> repositoryFactory, bool isBare = false)
        {
            Repository.Init(path, isBare);
            using (var repository = repositoryFactory())
            {
                var result = repository.Commit(AddMetadataObjectToCommit, message, signature, signature);
                SetRepositoryData(repositoryFactory, r => r.Head.Tip.Tree);
                return result;
            }
        }

        /// <inheritdoc />
        public Commit Commit(AbstractInstance newInstance, Signature signature, string message, CommitOptions options = null)
        {
            using (var repository = GetRepository())
            {
                var computeChanges = _computeTreeChangesFactory(GetRepository);
                var changes = computeChanges.Compare(this, newInstance, repository);
                return changes.AnyChange ?
                    repository.Commit(changes.NewTree, message, signature, signature, options) :
                    null;
            }
        }

        void AddMetadataObjectToCommit(Repository repository, TreeDefinition tree) =>
            AddNodeToCommit(repository, tree, new Stack<string>(), this);

        void AddNodeToCommit(Repository repository, TreeDefinition tree, Stack<string> stack, IMetadataObject node)
        {
            var path = stack.ToDataPath();
            node.ToJson(_jsonBuffer);
            tree.Add(path, repository.CreateBlob(_jsonBuffer), Mode.NonExecutableFile);
            AddNodeChildrenToCommit(repository, tree, stack, node);
        }

        void AddNodeChildrenToCommit(Repository repository, TreeDefinition tree, Stack<string> stack, IMetadataObject node)
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
