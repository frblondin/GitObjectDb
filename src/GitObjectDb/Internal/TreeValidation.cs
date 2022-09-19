using GitObjectDb.Model;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace GitObjectDb
{
    internal class TreeValidation : ITreeValidation
    {
        public void Validate(Tree tree, IDataModel model)
        {
            foreach (var item in tree)
            {
                ValidateNodeCollection(item, model);
            }
        }

        private void ValidateNodeCollection(TreeEntry entry, IDataModel model)
        {
            var types = model.GetTypesMatchingFolderName(entry.Name);
            if (!types.Any())
            {
                throw new GitObjectDbException($"No type matching folder name '{entry.Name}' could be found.");
            }
            var useNodeFolder = types.GroupBy(t => t.UseNodeFolders);
            ThrowIfDifferentNodeFolderValues(useNodeFolder);
            ValidateNodeCollectionChildren(entry, model, useNodeFolder.Single().Key);
        }

        private void ValidateNodeCollectionChildren(TreeEntry entry, IDataModel model, bool useNodeFolder)
        {
            if (useNodeFolder)
            {
                ValidateNodeCollectionChildrenUsingNodeFolder(entry, model);
            }
            else
            {
                ValidateNodeCollectionChildrenNotUsingNodeFolder(entry);
            }
        }

        private void ValidateNodeCollectionChildrenNotUsingNodeFolder(TreeEntry entry)
        {
            foreach (var item in entry.Target.Peel<Tree>())
            {
                switch (item.TargetType)
                {
                    case TreeEntryTargetType.Blob when item.Name.EndsWith(".json", StringComparison.Ordinal):
                        var nodeId = Path.GetFileNameWithoutExtension(item.Name);
                        ValidateNodeId(nodeId);
                        break;
                    case TreeEntryTargetType.Blob:
                        throw new GitObjectDbException($"A node collection with {nameof(GitFolderAttribute.UseNodeFolders)} = false should only contain nodes of type '<ParentNodeId>.json'. Blob entry {item.Name}' was not expected.");
                    case TreeEntryTargetType.Tree:
                        throw new GitObjectDbException($"A tree was not expected in a node collection that does not use {nameof(GitFolderAttribute.UseNodeFolders)}.");
                }
            }
        }

        private void ValidateNodeCollectionChildrenUsingNodeFolder(TreeEntry entry, IDataModel model)
        {
            foreach (var item in entry.Target.Peel<Tree>())
            {
                switch (item.TargetType)
                {
                    case TreeEntryTargetType.Tree when !item.Name.Equals(FileSystemStorage.ResourceFolder, StringComparison.Ordinal):
                        ValidateNodeFolder(item, model);
                        break;
                    case TreeEntryTargetType.Blob:
                        throw new NotSupportedException($"A blob was not expected to be found in a node collection using {nameof(GitFolderAttribute.UseNodeFolders)}.");
                    case TreeEntryTargetType.Tree when item.Name == FileSystemStorage.ResourceFolder:
                        throw new NotSupportedException($"A resource folder was not expected to be found in a node collection.");
                }
            }
        }

        private void ValidateNodeFolder(TreeEntry nodeFolder, IDataModel model)
        {
            var nodeFolderTree = nodeFolder.Target.Peel<Tree>();
            ValidateNodeId(nodeFolder.Name);
            var nodeDataFileFound = ValidateNodeFolderItems(nodeFolder.Name, nodeFolderTree, model);
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

        private bool ValidateNodeFolderItems(string id, Tree nodeFolderTree, IDataModel model)
        {
            foreach (var item in nodeFolderTree)
            {
                switch (item.TargetType)
                {
                    case TreeEntryTargetType.Tree when item.Name.Equals(FileSystemStorage.ResourceFolder, StringComparison.Ordinal):
                        ValidateResources(item, model);
                        break;
                    case TreeEntryTargetType.Tree:
                        ValidateNodeCollection(item, model);
                        break;
                    case TreeEntryTargetType.Blob when item.Name.Equals($"{id}.json", StringComparison.Ordinal):
                        return true;
                    case TreeEntryTargetType.Blob:
                        throw new GitObjectDbException($"A node folder should only contain a file named '<ParentNodeId>.json'. Blob entry '{item.Name}' was not expected.");
                }
            }
            return false;
        }

        [ExcludeFromCodeCoverage]
        private static void ThrowIfDifferentNodeFolderValues(IEnumerable<IGrouping<bool, NodeTypeDescription>> useNodeFolder)
        {
            if (useNodeFolder.Count() > 1)
            {
                throw new NotSupportedException($"A model containing several types with different values for {nameof(GitFolderAttribute.UseNodeFolders)} is not supported.");
            }
        }

        private void ValidateResources(TreeEntry entry, IDataModel model)
        {
            // Placeholder for future validations.
            // TODO: use FileSystemStorage.ThrowIfAnyReservedName
        }
    }
}
