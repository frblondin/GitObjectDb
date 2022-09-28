using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Queries;
using GitObjectDb.Model;
using GitObjectDb.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static GitObjectDb.Internal.Factories;

namespace GitObjectDb.Internal;

[DebuggerDisplay("{Repository}")]
internal sealed partial class Connection : IConnectionInternal, ISubmoduleProvider
{
    private readonly TransformationComposerFactory _transformationComposerFactory;
    private readonly RebaseFactory _rebaseFactory;
    private readonly MergeFactory _mergeFactory;
    private readonly CherryPickFactory _cherryPickFactory;
    private readonly IQuery<LoadItem.Parameters, ITreeItem> _loader;
    private readonly IQuery<QueryItems.Parameters, IEnumerable<(DataPath Path, Lazy<ITreeItem> Item)>> _queryItems;
    private readonly IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>> _queryResources;
    private readonly IQuery<SearchItems.Parameters, IEnumerable<(DataPath Path, ITreeItem Item)>> _searchItems;
    private readonly IComparerInternal _comparer;

    [FactoryDelegateConstructor(typeof(ConnectionFactory))]
    public Connection(string path,
                      IDataModel model,
                      string initialBranch,
                      IServiceProvider serviceProvider)
    {
        Repository = GetOrCreateRepository(path, initialBranch);
        Model = model;
        Cache = serviceProvider.GetService<IMemoryCache>();
        Serializer = serviceProvider.GetRequiredService<INodeSerializer.Factory>().Invoke(model);
        _transformationComposerFactory = serviceProvider.GetRequiredService<TransformationComposerFactory>();
        _rebaseFactory = serviceProvider.GetRequiredService<RebaseFactory>();
        _mergeFactory = serviceProvider.GetRequiredService<MergeFactory>();
        _cherryPickFactory = serviceProvider.GetRequiredService<CherryPickFactory>();
        _loader = serviceProvider.GetRequiredService<IQuery<LoadItem.Parameters, ITreeItem>>();
        _queryItems = serviceProvider.GetRequiredService<IQuery<QueryItems.Parameters, IEnumerable<(DataPath Path, Lazy<ITreeItem> Item)>>>();
        _queryResources = serviceProvider.GetRequiredService<IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>>>();
        _searchItems = serviceProvider.GetRequiredService<IQuery<SearchItems.Parameters, IEnumerable<(DataPath Path, ITreeItem Item)>>>();
        _comparer = serviceProvider.GetRequiredService<IComparerInternal>();
    }

    public IRepository Repository { get; }

    public IMemoryCache? Cache { get; }

    public INodeSerializer Serializer { get; }

    public IDataModel Model { get; }

    private static Repository GetOrCreateRepository(string path, string initialBranch)
    {
        var absolute = Path.GetFullPath(path);
        return !LibGit2Sharp.Repository.IsValid(absolute) ?
            InitializeRepository(absolute, initialBranch) :
            new Repository(absolute);
    }

    private static Repository InitializeRepository(string path, string initialBranch)
    {
        LibGit2Sharp.Repository.Init(path, isBare: true);
        var result = new Repository(path);
        result.Refs.UpdateTarget("HEAD", $"refs/heads/{initialBranch}");
        return result;
    }

    public ITransformationComposer Update(string branchName, Action<ITransformationComposer>? transformations = null)
    {
        var composer = _transformationComposerFactory(this, branchName);
        transformations?.Invoke(composer);
        return composer;
    }

    public IRebase Rebase(string branchName,
                          string upstreamCommittish,
                          ComparisonPolicy? policy = null) =>
        _rebaseFactory(this, branchName, upstreamCommittish, policy);

    public IMerge Merge(string branchName,
                        string upstreamCommittish,
                        ComparisonPolicy? policy = null) =>
        _mergeFactory(this, branchName, upstreamCommittish, policy);

    public ICherryPick CherryPick(string branchName,
                                  string committish,
                                  Signature? committer = null,
                                  CherryPickPolicy? policy = null) =>
        _cherryPickFactory(this, branchName, committish, committer, policy);

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
            return Repository.Branches[branch.UpstreamBranchCanonicalName].Tip;
        }
    }

    public void Dispose()
    {
        DisposeRepositories();
        Repository.Dispose();
    }

    private void DisposeRepositories()
    {
        foreach (var repository in _repositories.Values.ToList())
        {
            repository.Value.Dispose();
        }
        _repositories.Clear();
    }
}
