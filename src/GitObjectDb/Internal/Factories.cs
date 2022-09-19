using GitObjectDb.Comparison;
using LibGit2Sharp;

namespace GitObjectDb.Internal
{
    internal class Factories
    {
        internal delegate ITransformationComposer TransformationComposerFactory(IConnectionInternal connection);

        internal delegate IRebase RebaseFactory(IConnectionInternal connection, Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null);

        internal delegate IMerge MergeFactory(IConnectionInternal connection, Branch? branch = null, string? upstreamCommittish = null, ComparisonPolicy? policy = null);

        internal delegate ICherryPick CherryPickFactory(IConnectionInternal connection, string committish, Signature? committer, Branch? branch = null, CherryPickPolicy? policy = null);
    }
}
