using GitObjectDb.Comparison;
using LibGit2Sharp;

namespace GitObjectDb.Internal;

internal class Factories
{
    internal delegate ITransformationComposer TransformationComposerFactory(IConnectionInternal connection,
                                                                            string branchName,
                                                                            CommitCommandType commitType);

    internal delegate IRebase RebaseFactory(IConnectionInternal connection,
                                            string branchName,
                                            string upstreamCommittish,
                                            ComparisonPolicy? policy,
                                            CommitCommandType commitType);

    internal delegate IMerge MergeFactory(IConnectionInternal connection,
                                          string branchName,
                                          string upstreamCommittish,
                                          ComparisonPolicy? policy,
                                          CommitCommandType commitType);

    internal delegate ICherryPick CherryPickFactory(IConnectionInternal connection,
                                                    string branchName,
                                                    string committish,
                                                    Signature? committer,
                                                    CherryPickPolicy? policy,
                                                    CommitCommandType commitType);
}
