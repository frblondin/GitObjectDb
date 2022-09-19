using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Queries;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static GitObjectDb.Internal.Factories;

namespace GitObjectDb.Internal
{
    [DebuggerDisplay("{Repository}")]
    internal sealed class Connection : IConnectionInternal
    {
        private readonly TransformationComposerFactory _transformationComposerFactory;
        private readonly RebaseFactory _rebaseFactory;
        private readonly MergeFactory _mergeFactory;
        private readonly CherryPickFactory _cherryPickFactory;
        private readonly IQuery<LoadItem.Parameters, ITreeItem> _loader;
        private readonly IQuery<QueryNodes.Parameters, IEnumerable<Node>> _queryNodes;
        private readonly IQuery<QueryPaths.Parameters, IEnumerable<DataPath>> _queryPaths;
        private readonly IQuery<QueryResources.Parameters, IEnumerable<Resource>> _queryResources;
        private readonly IComparerInternal _comparer;

        [FactoryDelegateConstructor(typeof(ConnectionFactory))]
        public Connection(
            string path,
            string initialBranch,
            TransformationComposerFactory transformationComposerFactory,
            RebaseFactory rebaseFactory,
            MergeFactory mergeFactory,
            CherryPickFactory cherryPickFactory,
            IQuery<LoadItem.Parameters, ITreeItem> loader,
            IQuery<QueryNodes.Parameters, IEnumerable<Node>> queryNodes,
            IQuery<QueryPaths.Parameters, IEnumerable<DataPath>> queryPaths,
            IQuery<QueryResources.Parameters, IEnumerable<Resource>> queryResources,
            IComparerInternal comparer)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException($"'{nameof(path)}' cannot be null or empty.", nameof(path));
            }

            if (!Repository.IsValid(path))
            {
                InitializeRepository(path, initialBranch);
            }
            Repository = new Repository(path);
            _transformationComposerFactory = transformationComposerFactory;
            _rebaseFactory = rebaseFactory;
            _mergeFactory = mergeFactory;
            _cherryPickFactory = cherryPickFactory;
            _loader = loader;
            _queryNodes = queryNodes;
            _queryPaths = queryPaths;
            _queryResources = queryResources;
            _comparer = comparer;
        }

        public Repository Repository { get; }

        public RepositoryInformation Info => Repository.Info;

        public BranchCollection Branches => Repository.Branches;

        public Branch Head => Repository.Head;

        public IQueryableCommitLog Commits => Repository.Commits;

        private static void InitializeRepository(string path, string initialBranch)
        {
            Repository.Init(path);

            var head = Path.Combine(path, ".git", "HEAD");
            var content = File.ReadAllText(head);
            var newContent = content.Replace("refs/heads/master", $"refs/heads/{initialBranch}");
            File.WriteAllText(head, newContent);
        }

        public ITransformationComposer Update(Action<ITransformationComposer> transformations)
        {
            var composer = _transformationComposerFactory(this);
            transformations(composer);
            return composer;
        }

        public TItem Lookup<TItem>(DataPath path, string? committish = null, IDictionary<DataPath, ITreeItem>? referenceCache = null)
            where TItem : ITreeItem
        {
            var (tree, _) = GetTree(path, committish);
            return (TItem)_loader.Execute(this, new LoadItem.Parameters(tree, path, referenceCache));
        }

        public IEnumerable<Node> GetNodes(Node? parent = null, string? committish = null, bool isRecursive = false, IDictionary<DataPath, ITreeItem>? referenceCache = null)
        {
            var (tree, relativeTree) = GetTree(parent?.Path, committish);
            return _queryNodes.Execute(this, new QueryNodes.Parameters(typeof(Node), parent, tree, relativeTree, isRecursive, referenceCache));
        }

        public IEnumerable<TResult> GetNodes<TResult>(Node? parent = null, string? committish = null, bool isRecursive = false, IDictionary<DataPath, ITreeItem>? referenceCache = null)
            where TResult : Node
        {
            var (tree, relativeTree) = GetTree(parent?.Path, committish);
            foreach (var node in _queryNodes.Execute(this, new QueryNodes.Parameters(typeof(TResult), parent, tree, relativeTree, isRecursive, referenceCache)))
            {
                yield return (TResult)node;
            }
        }

        public IEnumerable<DataPath> GetPaths(DataPath? parentPath = null, string? committish = null, bool isRecursive = false)
        {
            return GetPaths<ITreeItem>(parentPath, committish, isRecursive);
        }

        public IEnumerable<DataPath> GetPaths<TItem>(DataPath? parentPath = null, string? committish = null, bool isRecursive = false)
            where TItem : ITreeItem
        {
            var (tree, relativeTree) = GetTree(parentPath, committish);
            return _queryPaths.Execute(this, new QueryPaths.Parameters(typeof(TItem), parentPath, tree, relativeTree, isRecursive));
        }

        private (Tree Tree, Tree RelativePath) GetTree(DataPath? path = null, string? committish = null)
        {
            var commit = committish != null ?
                (Commit)Repository.Lookup(committish) :
                Head.Tip;
            if (commit == null)
            {
                throw new GitObjectDbException("No valid commit could be found.");
            }
            if (path is null || string.IsNullOrEmpty(path.FolderPath))
            {
                return (commit.Tree, commit.Tree);
            }
            else
            {
                var tree = commit.Tree[path.FolderPath] ?? throw new GitObjectDbException("Requested path could not be found.");
                return (commit.Tree, tree.Target.Peel<Tree>());
            }
        }

        public IEnumerable<Resource> GetResources(Node node, string? committish = null)
        {
            if (node.Path is null)
            {
                throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.");
            }

            var (_, relativeTree) = GetTree(node.Path, committish);
            return _queryResources.Execute(this, new QueryResources.Parameters(relativeTree, node.Path));
        }

        public ChangeCollection Compare(string startCommittish, string? committish = null, ComparisonPolicy? policy = null)
        {
            var (old, _) = GetTree(committish: startCommittish);
            var (@new, _) = GetTree(committish: committish);
            return _comparer.Compare(this, old, @new, policy ?? ComparisonPolicy.Default);
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

        public IRebase Rebase(Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null) =>
            _rebaseFactory(this, branch, upstreamCommittish, policy);

        public IMerge Merge(Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null) =>
            _mergeFactory(this, branch, upstreamCommittish, policy);

        public ICherryPick CherryPick(string committish, Signature? committer = null, Branch? branch = null, CherryPickPolicy? policy = null) =>
            _cherryPickFactory(this, committish, committer, branch, policy);

        public Commit FindUpstreamCommit(string? committish, Branch branch)
        {
            if (committish != null)
            {
                return (Commit)Repository.Lookup(committish) ??
                    throw new GitObjectDbException($"Upstream commit '{committish}' could not be resolved.");
            }
            else if (string.IsNullOrEmpty(branch.UpstreamBranchCanonicalName))
            {
                throw new GitObjectDbException("Branch has no upstream branch defined.");
            }
            else
            {
                return Branches[branch.UpstreamBranchCanonicalName].Tip;
            }
        }

        public void Dispose()
        {
            Repository.Dispose();
        }
    }
}
