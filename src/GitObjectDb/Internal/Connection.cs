using GitDotNet;
using GitObjectDb.Comparison;
using GitObjectDb.Injection;
using GitObjectDb.Internal.Queries;
using GitObjectDb.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static GitObjectDb.Internal.Factories;

namespace GitObjectDb.Internal;

[DebuggerDisplay("{Repository}")]
internal sealed partial class Connection : IConnectionInternal, ISubmoduleProvider
{
    private readonly GitConnectionProvider _connectionFactory;
    private readonly ChangeComposerFactory _transformationComposerFactory;
    private readonly IndexFactory _indexFactory;
    private readonly RebaseFactory _rebaseFactory;
    private readonly MergeFactory _mergeFactory;
    private readonly CherryPickFactory _cherryPickFactory;
    private readonly IAsyncQuery<LoadItem.Parameters, TreeItem?> _loader;
    private readonly IAsyncEnumerableQuery<QueryItems.Parameters, (DataPath Path, TreeItem Item)> _queryItems;
    private readonly IAsyncEnumerableQuery<QueryResources.Parameters, (DataPath Path, Resource Resource)> _queryResources;
    private readonly IAsyncEnumerableQuery<SearchItems.Parameters, (DataPath Path, TreeItem Item)> _searchItems;
    private readonly IComparerInternal _comparer;

    [FactoryDelegate(typeof(ConnectionFactory))]
    public Connection(string path,
        IDataModel model,
        IServiceProvider serviceProvider)
    {
        var connectionProvider = serviceProvider.GetRequiredService<GitConnectionProvider>();
        Repository = GetOrCreateRepository(path, connectionProvider);
        Model = model;
        Cache = serviceProvider.GetRequiredService<IMemoryCache>();
        Serializer = serviceProvider.GetRequiredService<INodeSerializer>();
        _connectionFactory = serviceProvider.GetRequiredService<GitConnectionProvider>();
        _transformationComposerFactory = serviceProvider.GetRequiredService<ChangeComposerFactory>();
        _indexFactory = serviceProvider.GetRequiredService<IndexFactory>();
        _rebaseFactory = serviceProvider.GetRequiredService<RebaseFactory>();
        _mergeFactory = serviceProvider.GetRequiredService<MergeFactory>();
        _cherryPickFactory = serviceProvider.GetRequiredService<CherryPickFactory>();
        _loader = serviceProvider.GetRequiredService<IAsyncQuery<LoadItem.Parameters, TreeItem?>>();
        _queryItems = serviceProvider.GetRequiredService<IAsyncEnumerableQuery<QueryItems.Parameters, (DataPath Path, TreeItem Item)>>();
        _queryResources = serviceProvider.GetRequiredService<IAsyncEnumerableQuery<QueryResources.Parameters, (DataPath Path, Resource Resource)>>();
        _searchItems = serviceProvider.GetRequiredService<IAsyncEnumerableQuery<SearchItems.Parameters, (DataPath Path, TreeItem Item)>>();
        _comparer = serviceProvider.GetRequiredService<IComparerInternal>();

        ValidateModel();
    }

    ~Connection()
    {
        Dispose();
    }

    public IGitConnection Repository { get; }

    public IMemoryCache Cache { get; }

    public INodeSerializer Serializer { get; }

    public IDataModel Model { get; }

    private static IGitConnection GetOrCreateRepository(string path, GitConnectionProvider connectionProvider)
    {
        var absolute = Path.GetFullPath(path);
        if (!GitConnection.IsValid(absolute))
        {
            GitConnection.Create(path, isBare: true);
        }
        return connectionProvider.Invoke(path);
    }

    public async Task<IChangeComposerWithCommit> UpdateAsync(string branchName,
        Func<IChangeComposer, Task>? transformations = null)
    {
        var composer = _transformationComposerFactory(this, branchName);
        if (transformations is not null)
        {
            await transformations.Invoke(composer).ConfigureAwait(false);
        }
        return composer;
    }

    public async Task<IIndex> GetIndexAsync(string branchName,
        Func<IChangeComposer, Task>? transformations = null)
    {
        var index = await _indexFactory(this, branchName).ConfigureAwait(false);
        if (transformations is not null)
        {
            await transformations.Invoke(index).ConfigureAwait(false);
        }
        return index;
    }

    public Task<IRebase> RebaseAsync(string branchName,
        string upstreamCommittish,
        ComparisonPolicy? policy = null) =>
        _rebaseFactory(this, branchName, upstreamCommittish, policy);

    public Task<IMerge> MergeAsync(string branchName,
        string upstreamCommittish,
        ComparisonPolicy? policy = null) =>
        _mergeFactory(this, branchName, upstreamCommittish, policy);

    public Task<ICherryPick> CherryPickAsync(string branchName,
        string committish,
        Signature? committer = null,
        CherryPickPolicy? policy = null) =>
        _cherryPickFactory(this, branchName, committish, committer, policy);

    public async Task<CommitEntry> FindUpstreamCommitAsync(string? committish, Branch branch)
    {
        if (committish != null)
        {
            return await Repository.GetCommittishAsync(committish).ConfigureAwait(false) ??
                throw new GitObjectDbException($"Upstream commit '{committish}' could not be resolved.");
        }
        else if (string.IsNullOrEmpty(branch.UpstreamBranchCanonicalName))
        {
            throw new GitObjectDbException("Branch has no upstream branch defined.");
        }
        else
        {
            return await Repository.Branches[branch.UpstreamBranchCanonicalName].GetTipAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
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

    private void ValidateModel()
    {
        foreach (var nodeDescription in Model.NodeTypes)
        {
            ValidateNodeType(nodeDescription);
        }
    }

    private void ValidateNodeType(NodeTypeDescription nodeType)
    {
        foreach (var (property, extension) in nodeType.StoredAsSeparateFilesProperties)
        {
            if (extension.Equals(Serializer.FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                throw new GitObjectDbException($"Attribute {nameof(StoreAsSeparateFileAttribute)} applied to property " +
                                               $"{property} must use an extension which is different from serializer file extension.");
            }
        }
    }
}
