using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Queries;
using GitObjectDb.Model;
using GitObjectDb.Serialization;
using GitObjectDb.Tools;
using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
                      IMemoryCache referenceCache,
                      IServiceProvider serviceProvider)
    {
        Repository = GetOrCreateRepository(path, initialBranch);
        Model = model;
        ReferenceCache = referenceCache;
        Serializer = serviceProvider.GetRequiredService<NodeSerializerFactory>().Invoke(model);
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

    public IMemoryCache? ReferenceCache { get; }

    public INodeSerializer Serializer { get; }

    public IDataModel Model { get; }

    private static Repository GetOrCreateRepository(string path, string initialBranch)
    {
        var absolute = Path.GetFullPath(path);
        if (!LibGit2Sharp.Repository.IsValid(absolute))
        {
            InitializeRepository(absolute, initialBranch);
        }
        return new Repository(absolute);
    }

    private static void InitializeRepository(string path, string initialBranch)
    {
        LibGit2Sharp.Repository.Init(path, isBare: true);

        if (!initialBranch.Equals("master", StringComparison.Ordinal))
        {
            if (GitCliCommand.IsGitInstalled)
            {
                GitCliCommand.Execute(path, $"symbolic-ref HEAD refs/heads/{initialBranch}");
            }
            else
            {
                var head = Path.Combine(path, "HEAD");
                var content = File.ReadAllText(head);
                var newContent = content.Replace("refs/heads/master", $"refs/heads/{initialBranch}");
                File.WriteAllText(head, newContent);
            }
        }
    }

    public ITransformationComposer Update(Action<ITransformationComposer>? transformations = null)
    {
        var composer = _transformationComposerFactory(this);
        transformations?.Invoke(composer);
        return composer;
    }

    public Branch Checkout(string branchName, string? committish = null)
    {
        var branch = Repository.Branches[branchName];
        if (branch == null)
        {
            var reflogName =
                committish ??
                (Repository.Refs.Head is SymbolicReference ? Repository.Head.FriendlyName : Repository.Head.Tip.Sha);
            branch = Repository.CreateBranch(branchName, reflogName);
        }
        Repository.Refs.MoveHeadTarget(branch.CanonicalName);
        return branch;
    }

    public IRebase Rebase(Branch? branch = null,
                          string? upstreamCommittish = null,
                          ComparisonPolicy? policy = null) =>
        _rebaseFactory(this, branch, upstreamCommittish, policy);

    public IMerge Merge(Branch? branch = null,
                        string? upstreamCommittish = null,
                        ComparisonPolicy? policy = null) =>
        _mergeFactory(this, branch, upstreamCommittish, policy);

    public ICherryPick CherryPick(string committish,
                                  Signature? committer = null,
                                  Branch? branch = null,
                                  CherryPickPolicy? policy = null) =>
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
