using GitDotNet;
using GitObjectDb.Comparison;
using System.Threading.Tasks;

namespace GitObjectDb.Internal;

internal class Factories
{
    internal delegate IChangeComposerWithCommit ChangeComposerFactory(IConnectionInternal connection,
                                                                                      string branchName);

    internal delegate Task<IIndex> IndexFactory(IConnectionInternal connection, string branchName);

    internal delegate Task<IRebase> RebaseFactory(IConnectionInternal connection,
        string branchName,
        string upstreamCommittish,
        ComparisonPolicy? policy);

    internal delegate Task<IMerge> MergeFactory(IConnectionInternal connection,
        string branchName,
        string upstreamCommittish,
        ComparisonPolicy? policy);

    internal delegate Task<ICherryPick> CherryPickFactory(IConnectionInternal connection,
        string branchName,
        string committish,
        Signature? committer,
        CherryPickPolicy? policy);
    }
