using Fasterflect;
using GitObjectDb.Model;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitObjectDb.Internal.Commands;

internal class GitUpdateCommand : IGitUpdateCommand
{
    private readonly IDataModel _model;
    private readonly INodeSerializer _serializer;

    public GitUpdateCommand(IDataModel model, INodeSerializer serializer)
    {
        _model = model;
        _serializer = serializer;
    }

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

    private void CreateOrUpdateNode(Node node,
                                    INodeSerializer serializer,
                                    StreamWriter writer,
                                    ICollection<string> commitIndex,
                                    Tree? tree,
                                    ModuleCommands modules)
    {
        using var stream = serializer.Serialize(node);
        AddBlob(node.Path!, stream, writer, commitIndex);

        CreateOrUpdatePropertiesStoredAsSeparateFiles(node, writer, tree, commitIndex);
        CreateOrUpdateNodeRemoteResource(node.ThrowIfNoPath(), node.RemoteResource, tree, commitIndex, modules);
    }

    private void CreateOrUpdatePropertiesStoredAsSeparateFiles(Node node,
        StreamWriter writer,
        Tree? tree,
        ICollection<string> commitIndex)
    {
        var nodePath = node.ThrowIfNoPath();
        var typeDescription = _model.GetDescription(node.GetType());
        foreach (var info in typeDescription.StoredAsSeparateFilesProperties)
        {
            var path = new DataPath(nodePath.FolderPath,
                $"{Path.GetFileNameWithoutExtension(nodePath.FileName)}.{info.Property.Name}.{info.Extension}",
                false);
            var value = (string?)Reflect.PropertyGetter(info.Property).Invoke(node);
            if (value is null)
            {
                Delete(path, _serializer);
            }
            else
            {
                AddBlob(path, new MemoryStream(Encoding.Default.GetBytes(value)), writer, commitIndex);
            }
        }

        DeletePropertyValuesStoredAsSeparateFolder(tree, commitIndex, nodePath, _serializer,
            file => !typeDescription.StoredAsSeparateFilesProperties.Any(info =>
                info.Property.Name.Equals(file.PropertyName, StringComparison.Ordinal) &&
                info.Extension.Equals(file.Extension, StringComparison.Ordinal)));
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

    internal static void AddBlob(DataPath path, Stream stream, StreamWriter writer, ICollection<string> commitIndex)
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
        var newItem = ValidateRename(item, newPath, _serializer);

        return (ApplyUpdate)Delegate.Combine(
            Delete(item.ThrowIfNoPath(), _serializer),
            CreateOrUpdate(newItem));
    }

    internal static TreeItem ValidateRename(TreeItem item, DataPath newPath, INodeSerializer serializer)
    {
        if (newPath.IsNode(serializer) != item is Node ||
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
                Id = new UniqueId(Path.GetFileNameWithoutExtension(newPath.FileName)),
                Path = newPath,
            },
            _ => item with { Path = newPath },
        };
    }

    ApplyUpdate IGitUpdateCommand.Delete(DataPath path) =>
        Delete(path, _serializer);

    public static ApplyUpdate Delete(DataPath path, INodeSerializer serializer) =>
        (reference, modules, _, _, commitIndex) =>
        {
            // For nodes, delete whole folder containing node and nested entries
            // For resources, only deleted resource
            if (path.IsNode(serializer) && path.UseNodeFolders)
            {
                DeleteNodeFolder(reference, commitIndex, path);
            }
            else
            {
                commitIndex.Add($"D {path.FilePath}");
                DeletePropertyValuesStoredAsSeparateFolder(reference, commitIndex, path, serializer);
            }

            modules.RemoveRecursively(path, serializer);
        };

    private static void DeleteNodeFolder(Tree? reference, IList<string> commitIndex, DataPath path)
    {
        var nested = reference?[path.FolderPath]?.Traverse(path.FolderPath);
        if (nested is not null)
        {
            foreach (var item in nested)
            {
                if (item.Entry.TargetType is TreeEntryTargetType.Blob or
                    TreeEntryTargetType.GitLink)
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

    private static void DeletePropertyValuesStoredAsSeparateFolder(Tree? reference, ICollection<string> commitIndex,
        DataPath path, INodeSerializer serializer, Predicate<(string PropertyName, string Extension)>? propertyNamePredicate = null)
    {
        if (!path.IsNode(serializer))
        {
            return;
        }

        var parentFolder = reference?[path.FolderPath]?.Target.Peel<Tree>();
        if (parentFolder is not null)
        {
            var nodeId = Regex.Escape(Path.GetFileNameWithoutExtension(path.FileName));
            var regex = new Regex($@"^{nodeId}\.(?<property>\w+)\.(?<extension>\w+)");
            foreach (var fileName in from entry in parentFolder
                     let match = regex.Match(entry.Name)
                     where match.Success
                     where propertyNamePredicate == null ||
                           propertyNamePredicate((match.Result("${property}"), match.Result("${extension}")))
                     select entry.Name)
            {
                commitIndex.Add($"D {path.FolderPath}/{fileName}");
            }
        }
    }
}
