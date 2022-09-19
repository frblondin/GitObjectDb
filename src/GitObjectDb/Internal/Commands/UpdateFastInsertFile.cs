using GitObjectDb.Serialization;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;

namespace GitObjectDb.Internal.Commands;

internal class UpdateFastInsertFile
{
    private readonly INodeSerializer _serializer;

    public UpdateFastInsertFile(INodeSerializer serializer)
    {
        _serializer = serializer;
    }

    internal ApplyUpdateFastInsert CreateOrUpdate(ITreeItem item) => (_, writer, commitIndex) =>
    {
        switch (item)
        {
            case Node node:
                CreateOrUpdateNode(node, writer, commitIndex);
                break;
            case Resource resource:
                CreateOrUpdateResource(resource, writer, commitIndex);
                break;
            default:
                throw new NotSupportedException();
        }
    };

    internal static ApplyUpdateFastInsert Delete(ITreeItem item) => (reference, _, commitIndex) =>
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
    };

    internal static ApplyUpdateFastInsert Delete(DataPath path) => (reference, _, commitIndex) =>
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
    };

    private static void DeleteNodeFolder(Tree? reference, IList<string> commitIndex, DataPath path)
    {
        var nested = reference?[path.FolderPath]?.Traverse(path.FolderPath);
        if (nested is not null)
        {
            foreach (var item in nested)
            {
                if (item.Entry.TargetType == TreeEntryTargetType.Blob)
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

    private void CreateOrUpdateNode(Node node, StreamWriter writer, IList<string> commitIndex)
    {
        using var stream = _serializer.Serialize(node);
        AddBlob(node.Path!, stream, writer, commitIndex);
    }

    private static void CreateOrUpdateResource(Resource resource, StreamWriter writer, IList<string> commitIndex)
    {
        var stream = resource.Embedded.GetContentStream();
        AddBlob(resource.Path!, stream, writer, commitIndex);
    }

    private static void AddBlob(DataPath path, Stream stream, StreamWriter writer, IList<string> commitIndex)
    {
        var mark = commitIndex.Count + 1;
        writer.WriteLine($"blob");
        writer.WriteLine($"mark :{mark}");
        writer.WriteLine($"data {stream.Length}");
        writer.Flush();
        stream.CopyTo(writer.BaseStream);
        writer.WriteLine();
        commitIndex.Add($"M 100644 :{mark} {path.FilePath}");
    }
}
