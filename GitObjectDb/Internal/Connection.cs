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
        private readonly IQuery<Path, string, Node> _queryNode;
        private readonly IQuery<Node, string, IEnumerable<Node>> _queryNodeChildren;
        private readonly IQuery<Tree, Stack<string>, IEnumerable<Node>> _queryTreeChildren;

        [FactoryDelegateConstructor(typeof(ConnectionFactory))]
        public Connection(
            string path,
            NodeTransformationComposerFactory transformationComposerFactory,
            NodeRebaseFactory rebaseFactory,
            NodeMergeFactory mergeFactory,
            IQuery<Path, string, Node> queryNode,
            IQuery<Node, string, IEnumerable<Node>> queryNodeChildren,
            IQuery<Tree, Stack<string>, IEnumerable<Node>> queryTreeChildren)
        {
            if (!Repository.IsValid(path))
            {
                Repository.Init(path);
            }
            Repository = new Repository(path);
            _nodeTransformationComposerFactory = transformationComposerFactory ?? throw new ArgumentNullException(nameof(transformationComposerFactory));
            _rebaseFactory = rebaseFactory ?? throw new ArgumentNullException(nameof(rebaseFactory));
            _mergeFactory = mergeFactory ?? throw new ArgumentNullException(nameof(mergeFactory));
            _queryNode = queryNode ?? throw new ArgumentNullException(nameof(queryNode));
            _queryNodeChildren = queryNodeChildren ?? throw new ArgumentNullException(nameof(queryNodeChildren));
            _queryTreeChildren = queryTreeChildren ?? throw new ArgumentNullException(nameof(queryTreeChildren));
        }

        public Repository Repository { get; }

        public BranchCollection Branches => Repository.Branches;

        public Branch Head => Repository.Head;

        public INodeTransformationComposer Update(Func<INodeTransformationComposer, INodeTransformationComposer> transformations)
        {
            var empty = _nodeTransformationComposerFactory(this);
            return transformations(empty);
        }

        public TNode Get<TNode>(Path path, string committish = null)
            where TNode : Node =>
            (TNode)_queryNode.Execute(Repository, path, committish);

        public IEnumerable<Node> GetNodes(Node parent = null, string committish = null) =>
            _queryNodeChildren.Execute(Repository, parent, committish);

        public IEnumerable<TNode> GetNodes<TNode>(Node parent, string committish = null)
            where TNode : Node =>
            GetNodes(parent, committish).OfType<TNode>();

        public IEnumerator<Node> GetEnumerator() =>
            _queryTreeChildren.Execute(Repository, Repository.Head.Tip.Tree, new Stack<string>()).GetEnumerator();

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

        public INodeRebase Rebase(Branch branch = null, string upstreamCommittish = null, NodeMergerPolicy policy = null) =>
            _rebaseFactory(this, branch, upstreamCommittish, policy);

        public INodeMerge Merge(Branch branch = null, string upstreamCommittish = null, NodeMergerPolicy policy = null) =>
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
