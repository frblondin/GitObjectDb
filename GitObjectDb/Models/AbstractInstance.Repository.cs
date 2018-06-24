using LibGit2Sharp;
using GitObjectDb.Models;
using GitObjectDb.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    public partial class AbstractInstance : IInstance
    {
        Func<Repository> _getRepository;
        internal Func<Repository, Tree> _getTree;
        readonly StringBuilder _jsonBuffer = new StringBuilder();

        internal void SetRepositoryData(Func<Repository> getRepository, Func<Repository, Tree> getTree)
        {
            _getRepository = getRepository;
            _getTree = getTree;
        }

        internal Repository GetRepository() =>
            _getRepository?.Invoke() ?? throw new NullReferenceException("The module is not attached to a repository.");

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

        public Commit Commit(AbstractInstance @new, Signature signature, string message, CommitOptions options = null)
        {
            using (var repository = GetRepository())
            {
                var computeChanges = _computeTreeChangesFactory(GetRepository);
                var changes = computeChanges.Compare(this, @new, repository);
                return changes.AnyChange ?
                    repository.Commit(changes.NewTree, message, signature, signature, options) :
                    null;
            }
        }

        void AddMetadataObjectToCommit(Repository repository, TreeDefinition tree) =>
            AddNodeToCommit(repository, tree, new Stack<string>(), this);

        void AddNodeToCommit(Repository repository, TreeDefinition tree, Stack<string> stack, IMetadataObject node)
        {
            var path = stack.ToPath();
            if (!string.IsNullOrEmpty(path)) path += "/";
            path += InstanceLoader.DataFile;

            node.ToJson(_jsonBuffer);
            tree.Add(path, repository.CreateBlob(_jsonBuffer), Mode.NonExecutableFile);
            AddNodeChildrenToCommit(repository, tree, stack, node);
        }

        void AddNodeChildrenToCommit(Repository repository, TreeDefinition tree, Stack<string> stack, IMetadataObject node)
        {
            var dataAccessor = _dataAccessorProvider.Get(node.GetType());
            foreach (var childProperty in dataAccessor.ChildProperties)
            {
                var children = childProperty.Accessor(node).Cast<IMetadataObject>();
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

        public IMetadataObject GetFromGitPath(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var chunks = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            IMetadataObject result = this;
            for (int i = 0; i < chunks.Length && result != null; i++)
            {
                var propertyInfo = _dataAccessorProvider.Get(result.GetType()).ChildProperties.FirstOrDefault(p =>
                    p.Property.Name.Equals(chunks[i], StringComparison.OrdinalIgnoreCase));
                if (propertyInfo == null || ++i >= chunks.Length) return null;

                var children = propertyInfo.Accessor(result);
                var guid = Guid.Parse(chunks[i]);
                result = children.Cast<IMetadataObject>().FirstOrDefault(c => c.Id == guid);
            }
            return result;
        }
    }
}
