using LibGit2Sharp;
using System;

namespace GitObjectDb.Internal.Commands;

internal class GitUpdateCommandUsingTree : IGitUpdateCommand
{
    Delegate IGitUpdateCommand.CreateOrUpdate(TreeItem item) => CreateOrUpdate(item);

    internal static ApplyUpdateTreeDefinition CreateOrUpdate(TreeItem item) =>
        (_, modules, serializer, database, definition) =>
        {
            switch (item)
            {
                case Node node:
                    CreateOrUpdateNode(node, modules, serializer, database, definition);
                    break;
                case Resource resource:
                    CreateOrUpdateResource(resource, database, definition);
                    break;
                default:
                    throw new NotSupportedException();
            }
        };

    Delegate IGitUpdateCommand.Delete(DataPath path) => Delete(path);

    internal static ApplyUpdateTreeDefinition Delete(DataPath path) =>
        (_, modules, _, _, definition) =>
        {
            // For nodes, delete whole folder containing node and nested entries
            // For resources, only deleted resource
            definition.Remove(path.IsNode && path.UseNodeFolders ? path.FolderPath : path.FilePath);
            modules.RemoveRecursively(path);
        };

    Delegate IGitUpdateCommand.Rename(TreeItem item, DataPath newPath) => Rename(item, newPath);

    internal static ApplyUpdateTreeDefinition Rename(TreeItem item, DataPath newPath)
    {
        var newItem = ValidateRename(item, newPath);

        return (ApplyUpdateTreeDefinition)Delegate.Combine(
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

    private static void CreateOrUpdateNode(Node node,
                                           ModuleCommands modules,
                                           INodeSerializer serializer,
                                           ObjectDatabase database,
                                           TreeDefinition definition)
    {
        using var stream = serializer.Serialize(node);
        var blob = database.CreateBlob(stream);
        var path = node.ThrowIfNoPath();
        definition.Add(path.FilePath, blob, Mode.NonExecutableFile);

        CreateOrUpdateNodeRemoteResource(node, definition, modules);
    }

    private static void CreateOrUpdateNodeRemoteResource(Node node,
                                                         TreeDefinition definition,
                                                         ModuleCommands modules)
    {
        var resourcePath = $"{node.ThrowIfNoPath().FolderPath}/{FileSystemStorage.ResourceFolder}";
        if (node.RemoteResource is not null)
        {
            modules[resourcePath] = new(resourcePath, node.RemoteResource.Repository, null);
            definition.AddGitLink(resourcePath, (ObjectId)node.RemoteResource.Sha);
        }
        else if (definition[resourcePath]?.TargetType == TreeEntryTargetType.GitLink)
        {
            modules.Remove(resourcePath);
            definition.Remove(resourcePath);
        }
    }

    private static void CreateOrUpdateResource(Resource resource,
                                               ObjectDatabase database,
                                               TreeDefinition definition)
    {
        var stream = resource.Embedded.GetContentStream();
        var blob = database.CreateBlob(stream);
        definition.Add(resource.ThrowIfNoPath().FilePath, blob, Mode.NonExecutableFile);
    }
}
