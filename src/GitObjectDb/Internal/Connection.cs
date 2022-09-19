using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Queries;
using GitObjectDb.Model;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        private readonly IQuery<QueryItems.Parameters, IEnumerable<(DataPath Path, Lazy<ITreeItem> Item)>> _queryItems;
        private readonly IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>> _queryResources;
        private readonly IComparerInternal _comparer;

        [FactoryDelegateConstructor(typeof(ConnectionFactory))]
        public Connection(
            string path,
            string initialBranch,
            IDataModel model,
            TransformationComposerFactory transformationComposerFactory,
            RebaseFactory rebaseFactory,
            MergeFactory mergeFactory,
            CherryPickFactory cherryPickFactory,
            IQuery<LoadItem.Parameters, ITreeItem> loader,
            IQuery<QueryItems.Parameters, IEnumerable<(DataPath Path, Lazy<ITreeItem> Item)>> queryItems,
            IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>> queryResources,
            IComparerInternal comparer)
        {
            Repository = GetOrCreateRepository(path, initialBranch);
            Model = model;
            _transformationComposerFactory = transformationComposerFactory;
            _rebaseFactory = rebaseFactory;
            _mergeFactory = mergeFactory;
            _cherryPickFactory = cherryPickFactory;
            _loader = loader;
            _queryResources = queryResources;
            _comparer = comparer;
            _queryItems = queryItems;
        }

        public Repository Repository { get; }

        [ExcludeFromCodeCoverage]
        public Configuration Config => Repository.Config;

        [ExcludeFromCodeCoverage]
        public RepositoryInformation Info => Repository.Info;

        [ExcludeFromCodeCoverage]
        public BranchCollection Branches => Repository.Branches;

        [ExcludeFromCodeCoverage]
        public Branch Head => Repository.Head;

        [ExcludeFromCodeCoverage]
        public IQueryableCommitLog Commits => Repository.Commits;

        public IDataModel Model { get; }

        private Repository GetOrCreateRepository(string path, string initialBranch)
        {
            var absolute = Path.GetFullPath(path);
            if (!Repository.IsValid(absolute))
            {
                InitializeRepository(absolute, initialBranch);
            }
            return new Repository(absolute);
        }

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

        public IEnumerable<TItem> GetItems<TItem>(Node? parent = null, string? committish = null, bool isRecursive = false, IDictionary<DataPath, ITreeItem>? referenceCache = null)
            where TItem : ITreeItem
        {
            var (tree, relativeTree) = GetTree(parent?.Path, committish);
            return _queryItems.Execute(this, new QueryItems.Parameters(typeof(TItem), parent?.Path, tree, relativeTree, isRecursive, referenceCache)).Select(i => (TItem)i.Item.Value);
        }

        public IEnumerable<TNode> GetNodes<TNode>(Node? parent = null, string? committish = null, bool isRecursive = false, IDictionary<DataPath, ITreeItem>? referenceCache = null)
            where TNode : Node
        {
            return GetItems<TNode>(parent, committish, isRecursive, referenceCache);
        }

        public IEnumerable<DataPath> GetPaths(DataPath? parentPath = null, string? committish = null, bool isRecursive = false)
        {
            return GetPaths<ITreeItem>(parentPath, committish, isRecursive);
        }

        public IEnumerable<DataPath> GetPaths<TItem>(DataPath? parentPath = null, string? committish = null, bool isRecursive = false)
            where TItem : ITreeItem
        {
            var (tree, relativeTree) = GetTree(parentPath, committish);
            return _queryItems.Execute(this, new QueryItems.Parameters(typeof(TItem), parentPath, tree, relativeTree, isRecursive, null)).Select(i => i.Path);
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

        public IEnumerable<Resource> GetResources(Node node, string? committish = null, IDictionary<DataPath, ITreeItem>? referenceCache = null)
        {
            if (node.Path is null)
            {
                throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.");
            }

            var (tree, relativeTree) = GetTree(node.Path, committish);
            return _queryResources.Execute(this, new QueryResources.Parameters(tree, relativeTree, node.Path, referenceCache)).Select(i => i.Resource.Value);
        }

        public ChangeCollection Compare(string startCommittish, string? committish = null, ComparisonPolicy? policy = null)
        {
            var (old, _) = GetTree(committish: startCommittish);
            var (@new, _) = GetTree(committish: committish);
            return _comparer.Compare(this, old, @new, policy ?? Model.DefaultComparisonPolicy);
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
