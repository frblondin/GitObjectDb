using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Queries;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static GitObjectDb.Internal.Factories;

namespace GitObjectDb.Internal
{
    [DebuggerDisplay("{Repository.Path,nq}")]
    internal sealed class Connection : IConnectionInternal
    {
        private readonly NodeTransformationComposerFactory _nodeTransformationComposerFactory;
        private readonly NodeRebaseFactory _rebaseFactory;
        private readonly NodeMergeFactory _mergeFactory;
        private readonly NodeQueryFetcherFactory _fetcherFactory;
        private readonly IQuery<Tree, DataPath, ITreeItem> _loader;
        private readonly IQuery<DataPath, Tree, IEnumerable<Resource>> _queryResources;

        [FactoryDelegateConstructor(typeof(ConnectionFactory))]
        public Connection(
            string path,
            NodeTransformationComposerFactory transformationComposerFactory,
            NodeRebaseFactory rebaseFactory,
            NodeMergeFactory mergeFactory,
            NodeQueryFetcherFactory fetcherFactory,
            IQuery<Tree, DataPath, ITreeItem> loader,
            IQuery<DataPath, Tree, IEnumerable<Resource>> queryResources)
        {
            if (!Repository.IsValid(path))
            {
                Repository.Init(path);
            }
            Repository = new Repository(path);
            _nodeTransformationComposerFactory = transformationComposerFactory;
            _rebaseFactory = rebaseFactory;
            _mergeFactory = mergeFactory;
            _loader = loader;
            _queryResources = queryResources;
            _fetcherFactory = fetcherFactory;
        }

        public Repository Repository { get; }

        public BranchCollection Branches => Repository.Branches;

        public Branch Head => Repository.Head;

        public IQueryableCommitLog Commits => Repository.Commits;

        public INodeTransformationComposer Update(Func<INodeTransformationComposer, INodeTransformationComposer> transformations)
        {
            var empty = _nodeTransformationComposerFactory(this);
            return transformations(empty);
        }

        public TItem Lookup<TItem>(DataPath path, string? committish = null)
            where TItem : ITreeItem
        {
            var tree = GetTree(path, committish);
            return (TItem)_loader.Execute(Repository, tree, path);
        }

        public IQueryable<Node> AsQueryable(Node? parent = null, string? committish = null, bool isRecursive = false)
        {
            var tree = GetTree(parent?.Path, committish);
            var fetcher = _fetcherFactory(Repository, tree, parent, isRecursive);
            return new NodeQuery<Node>(fetcher);
        }

        private Tree GetTree(DataPath? path = null, string? committish = null)
        {
            var commit = committish != null ?
                (Commit)Repository.Lookup(committish) :
                Head.Tip;
            return path is null || string.IsNullOrEmpty(path.FolderPath) ?
                commit.Tree :
                commit.Tree[path.FolderPath].Target.Peel<Tree>();
        }

        public IEnumerable<Resource> GetResources(Node node, string? committish = null)
        {
            if (node.Path is null)
            {
                throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.");
            }

            var tree = GetTree(node.Path, committish);
            return _queryResources.Execute(Repository, node.Path, tree);
        }

        public Branch Checkout(string branchName, string? committish = null)
        {
            var head = Head;
            var branch = Branches[branchName];
            if (branch == null)
            {
                var reflogName = committish ?? (Repository.Refs.Head is SymbolicReference ? head.FriendlyName : head.Tip.Sha);
                branch = Repository.CreateBranch(branchName, reflogName);
            }
            Repository.Refs.MoveHeadTarget(branch.CanonicalName);
            return branch;
        }

        public INodeRebase Rebase(Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null) =>
            _rebaseFactory(this, branch, upstreamCommittish, policy);

        public INodeMerge Merge(Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null) =>
            _mergeFactory(this, branch, upstreamCommittish, policy);

        public void Dispose()
        {
            Repository.Dispose();
        }
    }
}
