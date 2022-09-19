using GitObjectDb.Internal.Commands;
using GitObjectDb.Model;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace GitObjectDb;

internal class TreeValidation : ITreeValidation
{
    public void Validate(Tree tree, IDataModel model)
    {
        var modules = new ModuleCommands(tree);
        var path = new Stack<string>();
        foreach (var item in tree.Where(i => i.TargetType == TreeEntryTargetType.Tree))
        {
            path.Push(item.Name);
            ValidateNodeCollection(item, model, modules, path);
            path.Pop();
        }
    }

    private void ValidateNodeCollection(TreeEntry entry, IDataModel model, ModuleCommands modules, Stack<string> path)
    {
        var types = model.GetTypesMatchingFolderName(entry.Name);
        if (!types.Any())
        {
            throw new GitObjectDbException($"No type matching folder name '{entry.Name}' could be found.");
        }
        var useNodeFolder = types.GroupBy(t => t.UseNodeFolders);
        ThrowIfDifferentNodeFolderValues(useNodeFolder);
        ValidateNodeCollectionChildren(entry, model, useNodeFolder.Single().Key, modules, path);
    }

    private void ValidateNodeCollectionChildren(TreeEntry entry,
                                                IDataModel model,
                                                bool useNodeFolder,
                                                ModuleCommands modules,
                                                Stack<string> path)
    {
        if (useNodeFolder)
        {
            ValidateNodeCollectionChildrenUsingNodeFolder(entry, model, modules, path);
        }
        else
        {
            ValidateNodeCollectionChildrenNotUsingNodeFolder(entry, path);
        }
    }

    private static void ValidateNodeCollectionChildrenNotUsingNodeFolder(TreeEntry entry,
                                                                         Stack<string> path)
    {
        foreach (var item in entry.Target.Peel<Tree>())
        {
            path.Push(item.Name);
            switch (item.TargetType)
            {
                case TreeEntryTargetType.Blob when item.Name.EndsWith(".json", StringComparison.Ordinal):
                    var nodeId = Path.GetFileNameWithoutExtension(item.Name);
                    ValidateNodeId(nodeId);
                    break;
                case TreeEntryTargetType.Blob:
                    throw new GitObjectDbException($"A node collection with {nameof(GitFolderAttribute.UseNodeFolders)} = false " +
                        $"should only contain nodes of type '<ParentNodeId>.json'. Blob entry {item.Name}' was not expected.");
                case TreeEntryTargetType.Tree:
                case TreeEntryTargetType.GitLink:
                    throw new GitObjectDbException($"A tree or link was not expected in a node collection that does " +
                        $"not use {nameof(GitFolderAttribute.UseNodeFolders)}.");
                default:
                    throw new NotSupportedException(item.TargetType.ToString());
            }
            path.Pop();
        }
    }

    private void ValidateNodeCollectionChildrenUsingNodeFolder(TreeEntry entry,
                                                               IDataModel model,
                                                               ModuleCommands modules,
                                                               Stack<string> path)
    {
        foreach (var item in entry.Target.Peel<Tree>())
        {
            path.Push(item.Name);
            switch (item.TargetType)
            {
                case TreeEntryTargetType.Tree when !FileSystemStorage.IsResourceName(item.Name):
                    ValidateNodeFolder(item, model, modules, path);
                    break;
                case TreeEntryTargetType.Blob:
                    throw new NotSupportedException($"A blob was not expected to be found in a node collection " +
                        $"using {nameof(GitFolderAttribute.UseNodeFolders)}.");
                case TreeEntryTargetType.Tree when item.Name == FileSystemStorage.ResourceFolder:
                case TreeEntryTargetType.GitLink:
                    ThrowGitLinkOrResourceFolderNotExpected();
                    return;
                default:
                    throw new NotSupportedException(item.TargetType.ToString());
            }
            path.Pop();
        }
    }

    private void ValidateNodeFolder(TreeEntry nodeFolder, IDataModel model, ModuleCommands modules, Stack<string> path)
    {
        var nodeFolderTree = nodeFolder.Target.Peel<Tree>();
        ValidateNodeId(nodeFolder.Name);
        var nodeDataFileFound = ValidateNodeFolderItems(nodeFolder.Name, nodeFolderTree, model, modules, path);
        if (!nodeDataFileFound)
        {
            throw new GitObjectDbException($"Node data folder '{nodeFolder.Name}.json' could be found in {nodeFolder.Path}.");
        }
    }

    private static void ValidateNodeId(string nodeId)
    {
        if (!UniqueId.TryParse(nodeId, out _))
        {
            throw new GitObjectDbException($"Folder name '{nodeId}' could not be parsed as a valid {nameof(UniqueId)}.");
        }
    }

    private bool ValidateNodeFolderItems(string id,
                                         Tree nodeFolderTree,
                                         IDataModel model,
                                         ModuleCommands modules,
                                         Stack<string> path)
    {
        var result = false;
        foreach (var item in nodeFolderTree)
        {
            path.Push(item.Name);
            switch (item.TargetType)
            {
                case TreeEntryTargetType.Tree when FileSystemStorage.IsResourceName(item.Name):
                    ValidateResources(item, modules, path);
                    break;
                case TreeEntryTargetType.GitLink when FileSystemStorage.IsResourceName(item.Name):
                    ValidateLinkResources(item, modules, path);
                    break;
                case TreeEntryTargetType.Tree:
                    ValidateNodeCollection(item, model, modules, path);
                    break;
                case TreeEntryTargetType.Blob when item.Name.Equals($"{id}.json", StringComparison.Ordinal):
                    result = true;
                    break;
                case TreeEntryTargetType.Blob:
                    throw new GitObjectDbException($"A node folder should only contain a file named '<ParentNodeId>.json'. " +
                        $"Blob entry '{item.Name}' was not expected.");
                case TreeEntryTargetType.GitLink:
                    throw new GitObjectDbException($"A link folder is only valid as a resource. " +
                        $"Git link '{item.Name}' was not expected.");
                default:
                    throw new NotSupportedException($"{item.TargetType} is not supported.");
            }
            path.Pop();
        }
        return result;
    }

    [ExcludeFromCodeCoverage]
    private static void ThrowIfDifferentNodeFolderValues(IEnumerable<IGrouping<bool, NodeTypeDescription>> useNodeFolder)
    {
        if (useNodeFolder.Count() > 1)
        {
            throw new NotSupportedException($"A model containing several types with different values " +
                $"for {nameof(GitFolderAttribute.UseNodeFolders)} is not supported.");
        }
    }

    private static void ValidateResources(TreeEntry entry, ModuleCommands modules, Stack<string> path)
    {
        var gitPath = string.Join("/", path.Reverse());
        var traversed = entry.Traverse(gitPath, includeSelf: false);
        var hasForbiddenEntry = traversed.Any(e =>
            e.Entry.TargetType == TreeEntryTargetType.GitLink ||
            (e.Entry.TargetType == TreeEntryTargetType.Tree &&
             FileSystemStorage.IsResourceName(e.Entry.Name)));
        if (hasForbiddenEntry)
        {
            ThrowGitLinkOrResourceFolderNotExpected();
            return;
        }

        ThrowIfMixingEmbeddedAndLinkedResources(modules, path);
    }

    private static void ThrowGitLinkOrResourceFolderNotExpected()
    {
        throw new NotSupportedException($"A resource folder or link was not expected to be found in a node collection.");
    }

    private static void ThrowIfMixingEmbeddedAndLinkedResources(ModuleCommands modules, Stack<string> path)
    {
        var gitPath = string.Join("/", path.Reverse());
        var module = modules[gitPath];
        if (module is not null)
        {
            throw new NotSupportedException(
                $"Cannot mix embedded and linked resources for the same node {gitPath}");
        }
    }

    private static void ValidateLinkResources(TreeEntry entry,
                                              ModuleCommands modules,
                                              Stack<string> path)
    {
        var gitPath = string.Join("/", path.Reverse());
        var module = modules[gitPath];
        if (module is null)
        {
            throw new GitObjectDbException(
                $"Linked resource {gitPath} could not be found in .gitmodules.");
        }
    }
}
