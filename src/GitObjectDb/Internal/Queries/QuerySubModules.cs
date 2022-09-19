using LibGit2Sharp;
using System;
using System.Linq;

namespace GitObjectDb.Internal.Queries;

internal class QuerySubModules : IQuery<QuerySubModules.Parameters, Commit>
{
    public Commit Execute(IQueryAccessor queryAccessor, Parameters parameters)
    {
        var remoteResource = parameters.Node.RemoteResource ??
                             throw new GitObjectDbException("Can only query submodules for nodes using remote resources.");
        var submoduleProvider = queryAccessor as ISubmoduleProvider ??
                                throw new ArgumentException($"Can only query submodules when query accessor " +
                                                            $"implements {nameof(ISubmoduleProvider)}.");
        var repository = submoduleProvider.GetOrCreateSubmoduleRepository(parameters.Node.ThrowIfNoPath(),
                                                                          remoteResource.Repository);

        return repository.Lookup<Commit>(remoteResource.Sha) ??
               FetchRemote(repository, parameters).Lookup<Commit>(remoteResource.Sha) ??
               throw new GitObjectDbException($"GitLink commit {remoteResource.Sha} could not " +
                                              $"be found in remote repository {repository.Info.Path}.");
    }

    /// <summary>
    /// In the case where a node points to different remote repositories
    /// depending on the branches, we use multiple origins so we can fetch
    /// any commit from any remote repository.
    /// </summary>
    private static Repository FetchRemote(Repository repository, Parameters parameters)
    {
        var url = parameters.Node.RemoteResource!.Repository;
        var matchingOrigin =
            repository.Network.Remotes.FirstOrDefault(r =>
                r.Url.Equals(url, StringComparison.OrdinalIgnoreCase)) ??
            repository.Network.Remotes.Add(UniqueId.CreateNew().ToString(), url);
        LibGit2Sharp.Commands.Fetch(repository, matchingOrigin.Name, Array.Empty<string>(), null, null);
        return repository;
    }

    internal class Parameters
    {
        public Parameters(Node node)
        {
            Node = node;
        }

        public Node Node { get; }
    }
}
