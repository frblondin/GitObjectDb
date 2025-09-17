using Fasterflect;
using GitDotNet;
using GitObjectDb.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GitObjectDb.Internal.Commands;

internal class GitUpdateCommand(IDataModel model, INodeSerializer serializer) : IGitUpdateCommand
{
    public ApplyUpdate CreateOrUpdate(TreeItem item) =>
        async (tree, modules, serializer, composer) =>
        {
            switch (item)
            {
                case Node node:
                    await CreateOrUpdateNodeAsync(node, serializer, composer, tree, modules).ConfigureAwait(false);
                    break;
                case Resource resource:
                    await CreateOrUpdateResourceAsync(resource, composer).ConfigureAwait(false);
                    break;
                default:
                    throw new NotSupportedException();
            }
        };

    private async Task CreateOrUpdateNodeAsync(Node node,
        INodeSerializer serializer,
        ITransformationComposer composer,
        TreeEntry? tree,
        ModuleCommands modules)
    {
        var stream = serializer.Serialize(node);
        composer.AddOrUpdate(node.Path!.FilePath, stream);

        await CreateOrUpdatePropertiesStoredAsSeparateFilesAsync(node, composer, tree).ConfigureAwait(false);
        await CreateOrUpdateNodeRemoteResourceAsync(node.ThrowIfNoPath(), node.RemoteResource, tree, composer, modules).ConfigureAwait(false);
    }

    private async Task CreateOrUpdatePropertiesStoredAsSeparateFilesAsync(Node node,
        ITransformationComposer composer,
        TreeEntry? tree)
    {
        var nodePath = node.ThrowIfNoPath();
        var typeDescription = model.GetDescription(node.GetType());
        foreach (var (property, extension) in typeDescription.StoredAsSeparateFilesProperties)
        {
            var path = new DataPath(nodePath.FolderPath,
                $"{Path.GetFileNameWithoutExtension(nodePath.FileName)}.{property.Name}.{extension}",
                false);
            var value = (string?)Reflect.PropertyGetter(property).Invoke(node);
            if (value is null)
            {
                Delete(path, serializer);
            }
            else
            {
                composer.AddOrUpdate(path.FilePath, Encoding.Default.GetBytes(value));
            }
        }

        await DeletePropertyValuesStoredAsSeparateFolderAsync(tree, composer, nodePath, serializer,
            file => !typeDescription.StoredAsSeparateFilesProperties.Any(info =>
                info.Property.Name.Equals(file.PropertyName, StringComparison.Ordinal) &&
                info.Extension.Equals(file.Extension, StringComparison.Ordinal))).ConfigureAwait(false);
    }

    internal static async Task CreateOrUpdateNodeRemoteResourceAsync(DataPath nodePath,
        ResourceLink? link,
        TreeEntry? tree,
        ITransformationComposer composer,
        ModuleCommands modules)
    {
        var resourcePath = $"{nodePath.FolderPath}/{FileSystemStorage.ResourceFolder}";
        if (link is not null)
        {
            modules[resourcePath] = new(resourcePath, link.Repository, null);
            composer.AddOrUpdate(resourcePath, link.Sha, new GitDotNet.FileMode(ObjectType.GitLink));
        }
        else if (tree is not null && (await tree.GetFromPathAsync(resourcePath).ConfigureAwait(false))?.Mode.Type == ObjectType.GitLink)
        {
            modules.Remove(resourcePath);
            composer.Remove(resourcePath);
        }
    }

    private static async Task CreateOrUpdateResourceAsync(Resource resource, ITransformationComposer composer)
    {
        var stream = await resource.Embedded.GetContentStreamAsync().ConfigureAwait(false);
        composer.AddOrUpdate(resource.Path!.FilePath, stream);
    }

    internal static void AddBlob(DataPath path, byte[] data, ITransformationComposer composer)
    {
        composer.AddOrUpdate(path.FilePath, data);
    }

    public ApplyUpdate Rename(TreeItem item, DataPath newPath)
    {
        var newItem = ValidateRename(item, newPath, serializer);

        return (ApplyUpdate)Delegate.Combine(
            Delete(item.ThrowIfNoPath(), serializer),
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
        Delete(path, serializer);

    public static ApplyUpdate Delete(DataPath path, INodeSerializer serializer) =>
        async (reference, modules, _, composer) =>
        {
            // For nodes, delete whole folder containing node and nested entries
            // For resources, only deleted resource
            if (path.IsNode(serializer) && path.UseNodeFolders)
            {
                await DeleteNodeFolderAsync(reference, composer, path).ConfigureAwait(false);
            }
            else
            {
                composer.Remove(path.FilePath);
                await DeletePropertyValuesStoredAsSeparateFolderAsync(reference, composer, path, serializer).ConfigureAwait(false);
            }

            modules.RemoveRecursively(path, serializer);
        };

    private static async Task DeleteNodeFolderAsync(TreeEntry? reference, ITransformationComposer composer, DataPath path)
    {
        if (reference == null)
        {
            return;
        }
        var nested = await reference.GetFromPathAsync(path.FolderPath).ConfigureAwait(false);
        if (nested is not null)
        {
            var tree = await nested.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
            await foreach (var (p, entry) in tree.GetAllBlobEntriesAsync(path.FolderPath).ConfigureAwait(false))
            {
                if (entry.Mode.Type is ObjectType.RegularFile or ObjectType.GitLink)
                {
                    composer.Remove(p);
                }
            }
        }
        else
        {
            composer.Remove(path.FilePath);
        }
    }

    private static async Task DeletePropertyValuesStoredAsSeparateFolderAsync(TreeEntry? reference, GitDotNet.ITransformationComposer composer,
        DataPath path, INodeSerializer serializer, Predicate<(string PropertyName, string Extension)>? propertyNamePredicate = null)
    {
        if (!path.IsNode(serializer) || reference == null)
        {
            return;
        }

        var parentFolderItem = await reference.GetFromPathAsync(path.FolderPath).ConfigureAwait(false);
        if (parentFolderItem is not null)
        {
            var parentFolder = await parentFolderItem.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
            var nodeId = Regex.Escape(Path.GetFileNameWithoutExtension(path.FileName));
            var regex = new Regex($@"^{nodeId}\.(?<property>\w+)\.(?<extension>\w+)");
            foreach (var fileName in from entry in parentFolder.Children
                                     let match = regex.Match(entry.Name)
                                     where match.Success
                                     where propertyNamePredicate == null ||
                                           propertyNamePredicate((match.Result("${property}"), match.Result("${extension}")))
                                     select entry.Name)
            {
                composer.Remove($"{path.FolderPath}/{fileName}");
            }
        }
    }
}
