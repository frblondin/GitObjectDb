using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Queries;
using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static GitObjectDb.Internal.Factories;

namespace GitObjectDb.Internal
{
    internal sealed class Connection : IConnectionInternal
    {
        private readonly NodeTransformationComposerFactory _nodeTransformationComposerFactory;
        private readonly NodeRebaseFactory _rebaseFactory;
        private readonly NodeMergeFactory _mergeFactory;
        private readonly IQuery<Tree, DataPath, ITreeItem> _loader;
        private readonly IQuery<DataPath, Tree, IEnumerable<Node>> _queryNodes;
        private readonly IQuery<DataPath, Tree, IEnumerable<Resource>> _queryResources;

        [FactoryDelegateConstructor(typeof(ConnectionFactory))]
        public Connection(
            string path,
            NodeTransformationComposerFactory transformationComposerFactory,
            NodeRebaseFactory rebaseFactory,
            NodeMergeFactory mergeFactory,
            IQuery<Tree, DataPath, ITreeItem> loader,
            IQuery<DataPath, Tree, IEnumerable<Node>> queryNodes,
            IQuery<DataPath, Tree, IEnumerable<Resource>> queryResources)
        {
            if (!Repository.IsValid(path))
            {
                Repository.Init(path);
            }
            Repository = new Repository(path);
            _nodeTransformationComposerFactory = transformationComposerFactory ?? throw new ArgumentNullException(nameof(transformationComposerFactory));
            _rebaseFactory = rebaseFactory ?? throw new ArgumentNullException(nameof(rebaseFactory));
            _mergeFactory = mergeFactory ?? throw new ArgumentNullException(nameof(mergeFactory));
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
            _queryNodes = queryNodes ?? throw new ArgumentNullException(nameof(queryNodes));
            _queryResources = queryResources ?? throw new ArgumentNullException(nameof(queryResources));
        }

        public Repository Repository { get; }

        public BranchCollection Branches => Repository.Branches;

        public Branch Head => Repository.Head;

        public INodeTransformationComposer Update(Func<INodeTransformationComposer, INodeTransformationComposer> transformations)
        {
            var empty = _nodeTransformationComposerFactory(this);
            return transformations(empty);
        }

        public TNode Get<TNode>(DataPath path, string committish = null)
            where TNode : Node
        {
            var tree = GetTree(path, committish);
            return (TNode)_loader.Execute(Repository, tree, path);
        }

        public IEnumerable<Node> GetNodes(Node parent = null, string committish = null)
        {
            var tree = GetTree(parent?.Path, committish);
            return _queryNodes.Execute(Repository, parent?.Path, tree);
        }

        public IEnumerable<TNode> GetNodes<TNode>(Node parent, string committish = null)
            where TNode : Node =>
            GetNodes(parent, committish).OfType<TNode>();

        private Tree GetTree(DataPath path = null, string committish = null)
        {
            var commit = committish != null ?
                (Commit)Repository.Lookup(committish) :
                Repository.Head.Tip;
            return path == null || string.IsNullOrEmpty(path.FolderPath) ?
                commit.Tree :
                commit.Tree[path.FolderPath].Target.Peel<Tree>();
        }

        public IEnumerator<Node> GetEnumerator() =>
            GetNodes().GetEnumerator();

        public IEnumerable<Resource> GetResources(Node node, string committish = null)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var tree = GetTree(node.Path, committish);
            return _queryResources.Execute(Repository, node.Path, tree);
        }

        public Branch Checkout(string branchName, bool createNewBranch = false, string committish = null)
        {
            var head = Repository.Head;
            var branch = Repository.Branches[branchName];
            if (createNewBranch)
            {
                if (branch != null)
                {
                    throw new GitObjectDbException($"The branch '{branchName}' already exists.");
                }
                var reflogName = committish ?? (Repository.Refs.Head is SymbolicReference ? head.FriendlyName : head.Tip.Sha);
                branch = Repository.CreateBranch(branchName, reflogName);
            }
            else if (branch == null)
            {
                throw new GitObjectDbException($"The branch '{branchName}' does not exist.");
            }
            Repository.Refs.MoveHeadTarget(branch.CanonicalName);
            return branch;
        }

        public INodeRebase Rebase(Branch branch = null, string upstreamCommittish = null, ComparisonPolicy policy = null) =>
            _rebaseFactory(this, branch, upstreamCommittish, policy);

        public INodeMerge Merge(Branch branch = null, string upstreamCommittish = null, ComparisonPolicy policy = null) =>
            _mergeFactory(this, branch, upstreamCommittish, policy);

        public T Lookup<T>(string objectish)
            where T : GitObject =>
            Repository.Lookup<T>(objectish);

        public void Dispose()
        {
            Repository.Dispose();
        }
    }
}
