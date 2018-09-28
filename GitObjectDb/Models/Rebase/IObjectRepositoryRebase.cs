using LibGit2Sharp;
using System;

namespace GitObjectDb.Models.Rebase
{
    /// <summary>
    /// Creates a new instance implementing the <see cref="IObjectRepositoryRebase"/> interface.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <param name="onCompleted">Delegate that will be invoked when the rebase operation has completed.</param>
    /// <returns>The newly created instance.</returns>
    public delegate IObjectRepositoryRebase ObjectRepositoryRebaseFactory(IObjectRepository repository, Action onCompleted);

    /// <summary>
    /// Encapsulates a rebase operation.
    /// </summary>
    public interface IObjectRepositoryRebase
    {
        /// <summary>
        /// Start a rebase operation.
        /// </summary>
        /// <param name="upstreamBranchName">The starting commit to rebase.</param>
        /// <param name="committer">The <see cref="Identity"/> of who added the change to the repository.</param>
        /// <param name="options">The <see cref="RebaseOptions"/> that specify the rebase behavior.</param>
        /// <returns>A <see cref="ObjectRepositoryRebaseResult"/>.</returns>
        ObjectRepositoryRebaseResult Start(string upstreamBranchName, Identity committer, RebaseOptions options = null);
    }
}