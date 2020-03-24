using GitObjectDb.Comparison;
using GitObjectDb.Internal.Queries;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Internal
{
    internal class Factories
    {
        internal delegate INodeTransformationComposer NodeTransformationComposerFactory(IConnectionInternal connection);

        internal delegate INodeRebase NodeRebaseFactory(IConnectionInternal connection, Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null);

        internal delegate INodeMerge NodeMergeFactory(IConnectionInternal connection, Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null);

        internal delegate NodeQueryFetcher NodeQueryFetcherFactory(Repository repository, Tree tree, Node? parent, bool isRecursive);
    }
}
