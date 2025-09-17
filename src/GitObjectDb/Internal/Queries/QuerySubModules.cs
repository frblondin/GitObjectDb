using GitDotNet;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Internal.Queries;

internal class QuerySubModules : IAsyncQuery<QuerySubModules.Parameters, CommitEntry>
{
    public async Task<CommitEntry> ExecuteAsync(IConnection queryAccessor, Parameters parameters)
    {
        var remoteResource = parameters.Node.RemoteResource ??
                             throw new GitObjectDbException("Can only query submodules for nodes using remote resources.");
        var submoduleProvider = queryAccessor as ISubmoduleProvider ??
                                throw new ArgumentException($"Can only query submodules when query accessor " +
                                                            $"implements {nameof(ISubmoduleProvider)}.");
        var repository = submoduleProvider.GetOrCreateSubmoduleRepository(parameters.Node.ThrowIfNoPath(),
                                                                          remoteResource.Repository);

        return await repository.Objects.TryGetAsync<CommitEntry>(remoteResource.Sha).ConfigureAwait(false) ??
               await FetchRemote(repository, parameters).Objects.TryGetAsync<CommitEntry>(remoteResource.Sha).ConfigureAwait(false) ??
               throw new GitObjectDbException($"GitLink commit {remoteResource.Sha} could not " +
                                              $"be found in remote repository {repository.Info.Path}.");
    }

    /// <summary>
    /// In the case where a node points to different remote repositories
    /// depending on the branches, we use multiple origins so we can fetch
    /// any commit from any remote repository.
    /// </summary>
    private static IGitConnection FetchRemote(IGitConnection repository, Parameters parameters)
    {
        var url = parameters.Node.RemoteResource!.Repository;
        var remote =
            repository.Remotes.FirstOrDefault(r =>
                r.Url?.Equals(url, StringComparison.OrdinalIgnoreCase) ?? false) ??
            repository.Remotes.Add(UniqueId.CreateNew().ToString(), url);
        remote.Fetch();
        return repository;
    }

    internal record struct Parameters(Node Node);
}
