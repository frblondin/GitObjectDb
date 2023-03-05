using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace GitObjectDb.Internal.Commands;

internal class GitUpdateCommand : IGitUpdateCommand
{
    public ApplyUpdate CreateOrUpdate(TreeItem item) =>
        (tree, modules, serializer, writer, commitIndex) =>
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

    private static void CreateOrUpdateNode(Node node,
                                           INodeSerializer serializer,
                                           StreamWriter writer,
                                           ICollection<string> commitIndex,
                                           Tree? tree,
                                           ModuleCommands modules)
    {
        using var stream = serializer.Serialize(node);
        AddBlob(node.Path!, stream, writer, commitIndex);

        CreateOrUpdateNodeRemoteResource(node.ThrowIfNoPath(), node.RemoteResource, tree, commitIndex, modules);
    }

    internal static void CreateOrUpdateNodeRemoteResource(DataPath nodePath,
                                                         ResourceLink? link,
                                                         Tree? tree,
                                                         ICollection<string> commitIndex,
                                                         ModuleCommands modules)
    {
        var resourcePath = $"{nodePath.FolderPath}/{FileSystemStorage.ResourceFolder}";
        if (link is not null)
        {
            modules[resourcePath] = new(resourcePath, link.Repository, null);
            commitIndex.Add($"M 160000 {link.Sha} {resourcePath}");
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

    public ApplyUpdate Rename(TreeItem item, DataPath newPath)
    {
        var newItem = ValidateRename(item, newPath);

        return (ApplyUpdate)Delegate.Combine(
            Delete(item.ThrowIfNoPath()),
            CreateOrUpdate(newItem));
    }

    internal static TreeItem ValidateRename(TreeItem item, DataPath newPath)
    {
        if (newPath.IsNode != item is Node ||
            newPath.UseNodeFolders != item.ThrowIfNoPath().UseNodeFolders)
        {
            throw new GitObjectDbException("New rename path doesn't match the item type.");
        }

        if (newPath.UseNodeFolders)
        {
            throw new GitObjectDbException("Renaming nodes that can contain children is not supported.");
        }

        return item switch
        {
            Node n => n with
            {
                Id = new UniqueId(System.IO.Path.GetFileNameWithoutExtension(newPath.FileName)),
                Path = newPath,
            },
            _ => item with { Path = newPath },
        };
    }

    ApplyUpdate IGitUpdateCommand.Delete(DataPath path) =>
        Delete(path);

    public static ApplyUpdate Delete(DataPath path) =>
        (reference, modules, _, _, commitIndex) =>
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
}
