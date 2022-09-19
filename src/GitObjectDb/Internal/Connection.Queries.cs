using GitObjectDb.Comparison;
using LibGit2Sharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitObjectDb.Internal;

internal sealed partial class Connection
{
    // Use lazy for concurrent dictionary thread safety
    private readonly ConcurrentDictionary<DataPath, Lazy<Repository>> _repositories = new();

    public TItem? Lookup<TItem>(DataPath path,
                               string? committish = null)
        where TItem : ITreeItem
    {
        var (commit, _) = GetTree(path, committish);
        return commit.Tree[path.FilePath] is null ?
            default :
            (TItem)_loader.Execute(this, new(commit.Tree, path));
    }

    public TItem? Lookup<TItem>(UniqueId id,
                               string? committish = null)
        where TItem : ITreeItem
    {
        var (commit, _, path) = TryGetTree(id, committish);
        return path is null ?
            default :
            (TItem)_loader.Execute(this, new(commit.Tree, path));
    }

    public ICommitEnumerable<TItem> GetItems<TItem>(Node? parent = null,
                                                    string? committish = null,
                                                    bool isRecursive = false)
        where TItem : ITreeItem
    {
        var (commit, relativeTree) = GetTree(parent?.Path, committish);
        return _queryItems
            .Execute(this, new(commit.Tree,
                               relativeTree,
                               typeof(TItem),
                               parent?.Path,
                               isRecursive))
            .AsParallel()
                .Select(i => i.Item.Value)
                .OfType<TItem>()
                .OrderBy(i => i.Path)
            .AsSequential()
            .ToCommitEnumerable(commit.Id);
    }

    public ICommitEnumerable<TNode> GetNodes<TNode>(Node? parent = null,
                                                    string? committish = null,
                                                    bool isRecursive = false)
        where TNode : Node
    {
        return GetItems<TNode>(parent, committish, isRecursive);
    }

    public IEnumerable<DataPath> GetPaths(DataPath? parentPath = null,
                                          string? committish = null,
                                          bool isRecursive = false)
    {
        return GetPaths<ITreeItem>(parentPath, committish, isRecursive);
    }

    public IEnumerable<DataPath> GetPaths<TItem>(DataPath? parentPath = null,
                                                 string? committish = null,
                                                 bool isRecursive = false)
        where TItem : ITreeItem
    {
        var (commit, relativeTree) = GetTree(parentPath, committish);
        return _queryItems.Execute(this,
                                   new(commit.Tree,
                                       relativeTree,
                                       typeof(TItem),
                                       parentPath,
                                       isRecursive)).Select(i => i.Path);
    }

    public IEnumerable<ITreeItem> Search(string pattern,
                                        DataPath? parentPath = null,
                                        string? committish = null,
                                        bool ignoreCase = false,
                                        bool recurseSubModules = false)
    {
        var (commit, _) = GetTree(parentPath, committish);
        return _searchItems
            .Execute(this, new(this,
                               commit.Tree,
                               pattern,
                               parentPath,
                               committish,
                               ignoreCase,
                               recurseSubModules))
            .Select(i => i.Item);
    }

    public ICommitEnumerable<Resource> GetResources(Node node, string? committish = null)
    {
        if (node.Path is null)
        {
            throw new ArgumentNullException(nameof(node), $"{nameof(Node.Path)} is null.");
        }

        var (commit, relativeTree) = GetTree(node.Path, committish);
        return _queryResources
            .Execute(this, new(commit.Tree, relativeTree, node))
            .AsParallel()
                .Select(i => i.Resource.Value)
                .OrderBy(i => i.Path)
            .AsSequential()
            .ToCommitEnumerable(commit.Id);
    }

    public ChangeCollection Compare(string startCommittish,
                                    string? committish = null,
                                    ComparisonPolicy? policy = null)
    {
        var (old, _) = GetTree(committish: startCommittish);
        var (@new, _) = GetTree(committish: committish);
        return _comparer.Compare(this, old, @new, policy ?? Model.DefaultComparisonPolicy);
    }

    public IEnumerable<LogEntry> GetCommits(Node node, string? branch = null)
    {
        var filePath = node.ThrowIfNoPath().FilePath;
        var filter = new CommitFilter
        {
            IncludeReachableFrom = branch ?? "HEAD",
        };
        return Repository.Commits.QueryBy(filePath, filter);
    }

    private (Commit Commit, Tree RelativePath) GetTree(DataPath? path = null, string? committish = null)
    {
        var commit = committish != null ?
            (Commit)Repository.Lookup(committish) :
            Repository.Head.Tip;
        if (commit is null)
        {
            throw new GitObjectDbInvalidCommitException();
        }
        if (path is null || string.IsNullOrEmpty(path.FolderPath))
        {
            return (commit, commit.Tree);
        }
        else
        {
            var tree = commit.Tree[path.FolderPath] ??
                       throw new GitObjectDbException("Requested path could not be found.");
            return (commit, tree.Target.Peel<Tree>());
        }
    }

    private (Commit Commit, Tree? RelativePath, DataPath? Path) TryGetTree(UniqueId id, string? committish = null)
    {
        var commit = committish != null ?
            (Commit)Repository.Lookup(committish) :
            Repository.Head.Tip;
        if (commit is null)
        {
            throw new GitObjectDbInvalidCommitException();
        }

        var stack = new Stack<string>();
        var tree = Search(commit.Tree, $"{id}.{Serializer.FileExtension}", stack);
        var path = DataPath.Parse(string.Join("/", stack.Reverse()));
        return (commit, tree, path);

        static Tree? Search(Tree tree, string blobName, Stack<string> path)
        {
            foreach (var item in tree)
            {
                path.Push(item.Name);
                if (item.TargetType == TreeEntryTargetType.Blob && item.Name == blobName)
                {
                    return tree;
                }
                if (item.TargetType == TreeEntryTargetType.Tree &&
                    !FileSystemStorage.IsResourceName(item.Name))
                {
                    var found = Search(item.Target.Peel<Tree>(), blobName, path);
                    if (found is not null)
                    {
                        return found;
                    }
                }
                path.Pop();
            }
            return null;
        }
    }

    Repository ISubmoduleProvider.GetOrCreateSubmoduleRepository(DataPath path,
                                                                 string url)
    {
        var folderPath = Path.Combine(Repository.Info.Path,
                                      "modules",
                                      path.FolderPath,
                                      FileSystemStorage.ResourceFolder);
        return _repositories.GetOrAdd(path,
                                      new Lazy<Repository>(CreateOrLoad)).Value;
        Repository CreateOrLoad() =>
            LibGit2Sharp.Repository.IsValid(folderPath) ? Load() : Create();

        Repository Load() =>
            new(folderPath);

        Repository Create()
        {
            Directory.CreateDirectory(folderPath);
            LibGit2Sharp.Repository.Clone(url, folderPath, new()
            {
                IsBare = true,
            });
            return Load();
        }
    }
}
