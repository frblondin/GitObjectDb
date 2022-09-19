using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace GitObjectDb.Internal.Commands;

internal class UpdateFastInsertFile
{
    internal ApplyUpdateFastInsert CreateOrUpdate(ITreeItem item) => (tree, modules, serializer, writer, commitIndex) =>
    {
        switch (item)
        {
            case Node node:
                CreateOrUpdateNode(node, serializer, writer, commitIndex, tree, modules);
                break;
            case Resource resource:
                CreateOrUpdateResource(resource, writer, commitIndex);
                break;
            default:
                throw new NotSupportedException();
        }
    };

    internal static ApplyUpdateFastInsert Delete(ITreeItem item) => (reference, modules, _, _, commitIndex) =>
    {
        var path = item.ThrowIfNoPath();

        // For nodes, delete whole folder containing node and nested entries
        // For resources, only deleted resource
        if (item is Node && item.Path!.UseNodeFolders)
        {
            DeleteNodeFolder(reference, commitIndex, path);
        }
        else
        {
            commitIndex.Add($"D {path.FilePath}");
        }
        modules.RemoveRecursively(path);
    };

    internal static ApplyUpdateFastInsert Delete(DataPath path) => (reference, modules, _, _, commitIndex) =>
    {
        // For nodes, delete whole folder containing node and nested entries
        // For resources, only deleted resource
        if (path.IsNode && path.UseNodeFolders)
        {
            DeleteNodeFolder(reference, commitIndex, path);
        }
        else
        {
            commitIndex.Add($"D {path.FilePath}");
        }
        modules.RemoveRecursively(path);
    };

    private static void DeleteNodeFolder(Tree? reference, IList<string> commitIndex, DataPath path)
    {
        var nested = reference?[path.FolderPath]?.Traverse(path.FolderPath);
        if (nested is not null)
        {
            foreach (var item in nested)
            {
                if (item.Entry.TargetType == TreeEntryTargetType.Blob ||
                    item.Entry.TargetType == TreeEntryTargetType.GitLink)
                {
                    commitIndex.Add($"D {item.Path}");
                }
            }
        }
        else
        {
            commitIndex.Add($"D {path.FilePath}");
        }
    }

    private void CreateOrUpdateNode(Node node,
                                    INodeSerializer serializer,
                                    StreamWriter writer,
                                    ICollection<string> commitIndex,
                                    Tree? tree,
                                    ModuleCommands modules)
    {
        using var stream = serializer.Serialize(node);
        AddBlob(node.Path!, stream, writer, commitIndex);

        CreateOrUpdateNodeRemoteResource(node, tree, commitIndex, modules);
    }

    private static void CreateOrUpdateNodeRemoteResource(Node node,
                                                  Tree? tree,
                                                  ICollection<string> commitIndex,
                                                  ModuleCommands modules)
    {
        var resourcePath = $"{node.ThrowIfNoPath().FolderPath}/{FileSystemStorage.ResourceFolder}";
        if (node.RemoteResource is not null)
        {
            modules[resourcePath] = new(resourcePath, node.RemoteResource.Repository, null);
            commitIndex.Add($"M 160000 {node.RemoteResource.Sha} {resourcePath}");
        }
        else if (tree is not null && tree[resourcePath]?.TargetType == TreeEntryTargetType.GitLink)
        {
            modules.Remove(resourcePath);
            commitIndex.Add($"D {resourcePath}");
        }
    }

    private static void CreateOrUpdateResource(Resource resource, StreamWriter writer, ICollection<string> commitIndex)
    {
        var stream = resource.Embedded.GetContentStream();
        AddBlob(resource.Path!, stream, writer, commitIndex);
    }

    private static void AddBlob(DataPath path, Stream stream, StreamWriter writer, ICollection<string> commitIndex)
    {
        AddBlob(path.FilePath, stream, writer, commitIndex);
    }

    internal static void AddBlob(string path, Stream stream, StreamWriter writer, ICollection<string> commitIndex)
    {
        var mark = commitIndex.Count + 1;
        writer.WriteLine($"blob");
        writer.WriteLine($"mark :{mark}");
        writer.WriteLine($"data {stream.Length}");
        writer.Flush();
        stream.CopyTo(writer.BaseStream);
        writer.WriteLine();
        commitIndex.Add($"M 100644 :{mark} {path}");
    }
}
