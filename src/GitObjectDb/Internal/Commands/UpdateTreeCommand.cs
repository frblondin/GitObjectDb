using LibGit2Sharp;
using System;
using System.IO;

namespace GitObjectDb.Internal.Commands;

internal static class UpdateTreeCommand
{
    internal static ApplyUpdateTreeDefinition CreateOrUpdate(ITreeItem item) =>
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

    internal static ApplyUpdateTreeDefinition Delete(ITreeItem item) =>
        (_, modules, _, _, definition) =>
        {
            var path = item.ThrowIfNoPath();

            // For nodes, delete whole folder containing node and nested entries
            // For resources, only deleted resource
            definition.Remove(item is Node && item.Path!.UseNodeFolders ? path.FolderPath : path.FilePath);
            modules.RemoveRecursively(item.ThrowIfNoPath());
        };

    internal static ApplyUpdateTreeDefinition Delete(DataPath path) =>
        (_, modules, _, _, definition) =>
        {
            // For nodes, delete whole folder containing node and nested entries
            // For resources, only deleted resource
            definition.Remove(path.IsNode && path.UseNodeFolders ? path.FolderPath : path.FilePath);
            modules.RemoveRecursively(path);
        };

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
        definition.Add(resource.Path.FilePath, blob, Mode.NonExecutableFile);
    }
}
