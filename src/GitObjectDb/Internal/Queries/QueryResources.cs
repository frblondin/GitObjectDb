using LibGit2Sharp;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Internal.Queries;

internal class QueryResources : IQuery<QueryResources.Parameters, IEnumerable<(DataPath Path, Lazy<Resource> Resource)>>
{
    private readonly IQuery<QuerySubModules.Parameters, Commit> _querySubModules;

    public QueryResources(IQuery<QuerySubModules.Parameters, Commit> querySubModules)
    {
        _querySubModules = querySubModules;
    }

    public IEnumerable<(DataPath Path, Lazy<Resource> Resource)> Execute(IQueryAccessor queryAccessor,
                                                                         Parameters parms)
    {
        var nodePath = parms.Node.Path ??
            throw new GitObjectDbException("Node has no path defined.");
        var referenceResourceTree = parms.RelativeTree[FileSystemStorage.ResourceFolder];

        if (referenceResourceTree?.TargetType == TreeEntryTargetType.Tree)
        {
            var traversed = referenceResourceTree.Traverse(
                $"{nodePath.FolderPath}/{FileSystemStorage.ResourceFolder}");
            return ResolveResources(traversed);
        }

        if (parms.Node.RemoteResource is not null &&
            referenceResourceTree?.TargetType == TreeEntryTargetType.GitLink)
        {
            return ResolveGitLinkResources(queryAccessor, parms, nodePath);
        }

        return Enumerable.Empty<(DataPath Path, Lazy<Resource> Resource)>();
    }

    private IEnumerable<(DataPath Path, Lazy<Resource> Resource)> ResolveGitLinkResources(IQueryAccessor queryAccessor,
                                                                                          Parameters parms,
                                                                                          DataPath nodePath)
    {
        var commit = _querySubModules.Execute(queryAccessor, new(parms.Node));
        var traversed = commit.Tree.Traverse($"{nodePath.FolderPath}/{FileSystemStorage.ResourceFolder}");
        return ResolveResources(traversed);
    }

    private static IEnumerable<(DataPath Path, Lazy<Resource> Resource)> ResolveResources(
        IEnumerable<(TreeEntry Entry, string Path)> traversed)
    {
        foreach (var (entry, path) in traversed)
        {
            if (entry.TargetType == TreeEntryTargetType.Blob)
            {
                var dataPath = DataPath.Parse(path);
                yield return
                (
                    dataPath,
                    new Lazy<Resource>(() =>
                    {
                        var blob = entry.Target.Peel<Blob>();
                        return new Resource(dataPath,
                                            new Resource.Data(() => blob.GetContentStream()));
                    })
                );
            }
        }
    }

    internal class Parameters
    {
        public Parameters(Tree tree,
                          Tree relativeTree,
                          Node node)
        {
            Tree = tree;
            RelativeTree = relativeTree;
            Node = node;
        }

        public Tree Tree { get; }

        public Tree RelativeTree { get; }

        public Node Node { get; }
    }
}
