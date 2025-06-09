using GitDotNet;
using GitObjectDb.Internal.Commands;
using GitObjectDb.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#pragma warning disable SA1201 // Elements should appear in the correct order

namespace GitObjectDb;

internal class TreeValidation : ITreeValidation
{
    public async Task ValidateAsync(TreeEntry tree, IDataModel model, INodeSerializer serializer)
    {
        await new TreeValidationVisitor(tree, model, serializer).ValidateAsync().ConfigureAwait(false);
    }

    private struct TreeValidationVisitor(TreeEntry tree, IDataModel model, INodeSerializer serializer)
    {
        private readonly TreeEntry _tree = tree;
        private readonly IDataModel _model = model;
        private readonly INodeSerializer _serializer = serializer;
        private ModuleCommands? _modules;

        /// <summary>
        /// Defines the types of validation work that can be performed.
        /// </summary>
        private enum ValidationWorkType
        {
            NodeCollection,
            NodeFolder,
            Resources,
        }

        /// <summary>
        /// Represents a work item for iterative tree validation processing.
        /// </summary>
        private readonly record struct ValidationWorkItem(
            TreeEntryItem Entry,
            Stack<string> PathStack,
            ValidationWorkType WorkType);

        public async Task ValidateAsync()
        {
            _modules = await ModuleCommands.GetAsync(_tree).ConfigureAwait(false);

            // Use iterative processing instead of recursion
            var processingQueue = new Queue<ValidationWorkItem>();
            var processedEntries = new HashSet<string>(); // Track processed entries by their full path to avoid infinite loops

            // Initialize queue with root tree children
            foreach (var item in _tree.Children.Where(i => i.Mode.Type == ObjectType.Tree))
            {
                var pathStack = new Stack<string>();
                pathStack.Push(item.Name);
                var workItem = new ValidationWorkItem(
                    Entry: item,
                    PathStack: pathStack,
                    WorkType: ValidationWorkType.NodeCollection);
                processingQueue.Enqueue(workItem);
            }

            // Process queue iteratively
            while (processingQueue.Count > 0)
            {
                var workItem = processingQueue.Dequeue();
                var fullPath = string.Join("/", workItem.PathStack.Reverse());

                // Skip if already processed to avoid infinite loops
                if (processedEntries.Contains(fullPath))
                {
                    continue;
                }
                processedEntries.Add(fullPath);

                switch (workItem.WorkType)
                {
                    case ValidationWorkType.NodeCollection:
                        await ProcessNodeCollectionAsync(workItem, processingQueue).ConfigureAwait(false);
                        break;
                    case ValidationWorkType.NodeFolder:
                        await ProcessNodeFolderAsync(workItem, processingQueue).ConfigureAwait(false);
                        break;
                    case ValidationWorkType.Resources:
                        await ProcessResourcesAsync(workItem).ConfigureAwait(false);
                        break;
                }
            }
        }

        private async Task ProcessNodeCollectionAsync(ValidationWorkItem workItem, Queue<ValidationWorkItem> processingQueue)
        {
            var types = _model.GetTypesMatchingFolderName(workItem.Entry.Name);
            if (!types.Any())
            {
                throw new GitObjectDbValidationException($"No type matching folder name '{workItem.Entry.Name}' could be found.");
            }
            var useNodeFolder = types.GroupBy(t => t.UseNodeFolders);
            ThrowIfDifferentNodeFolderValues(useNodeFolder);

            var useNodeFolders = useNodeFolder.Single().Key;

            if (useNodeFolders)
            {
                await ProcessNodeCollectionChildrenUsingNodeFolderAsync(workItem, processingQueue).ConfigureAwait(false);
            }
            else
            {
                await ProcessNodeCollectionChildrenNotUsingNodeFolderAsync(workItem).ConfigureAwait(false);
            }
        }

        private async Task ProcessNodeCollectionChildrenNotUsingNodeFolderAsync(ValidationWorkItem workItem)
        {
            var folder = await workItem.Entry.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
            foreach (var item in folder.Children)
            {
                var newPath = new Stack<string>(workItem.PathStack.Reverse());
                newPath.Push(item.Name);

                switch (item.Mode.Type)
                {
                    case ObjectType.RegularFile when item.Name.EndsWith($".{_serializer.FileExtension}", StringComparison.Ordinal):
                        var nodeId = Path.GetFileNameWithoutExtension(item.Name);
                        ValidateNodeId(nodeId);
                        break;
                    case ObjectType.RegularFile:
                        ValidateBlobIsExistingNodePropertyValue(item, folder);
                        break;
                    case ObjectType.Tree:
                    case ObjectType.GitLink:
                        throw new GitObjectDbValidationException($"A tree or link was not expected in a node collection that does " +
                            $"not use {nameof(GitFolderAttribute.UseNodeFolders)}.");
                    default:
                        throw new NotSupportedException($"{item.Mode.Type} is not supported.");
                }
            }
        }

        private readonly void ValidateBlobIsExistingNodePropertyValue(TreeEntryItem item, TreeEntry tree)
        {
            var propertyNodeId = Regex.Replace(item.Name, @"^(\w+)\.\w+\.\w+", "$1");
            var expectedNodeBlobName = $"{propertyNodeId}.{_serializer.FileExtension}";
            if (tree.Children.FirstOrDefault(x => x.Name == expectedNodeBlobName) == null)
            {
                throw new GitObjectDbValidationException($"The blob {item.Name} does not refer to a valid node property. " +
                                                         $"Could not find an existing node {expectedNodeBlobName}.");
            }
        }

        private static async Task ProcessNodeCollectionChildrenUsingNodeFolderAsync(ValidationWorkItem workItem, Queue<ValidationWorkItem> processingQueue)
        {
            var tree = await workItem.Entry.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
            foreach (var item in tree.Children)
            {
                var newPath = new Stack<string>(workItem.PathStack.Reverse());
                newPath.Push(item.Name);

                switch (item.Mode.Type)
                {
                    case ObjectType.Tree when !FileSystemStorage.IsResourceName(item.Name):
                        var nodeWorkItem = new ValidationWorkItem(
                            Entry: item,
                            PathStack: newPath,
                            WorkType: ValidationWorkType.NodeFolder);
                        processingQueue.Enqueue(nodeWorkItem);
                        break;
                    case ObjectType.RegularFile:
                        throw new GitObjectDbValidationException($"A blob was not expected to be found in a node collection " +
                            $"using {nameof(GitFolderAttribute.UseNodeFolders)}.");
                    case ObjectType.Tree when item.Name == FileSystemStorage.ResourceFolder:
                    case ObjectType.GitLink:
                        ThrowGitLinkOrResourceFolderNotExpected();
                        return;
                    default:
                        throw new NotSupportedException($"{item.Mode.Type} is not supported.");
                }
            }
        }

        private async Task ProcessNodeFolderAsync(ValidationWorkItem workItem, Queue<ValidationWorkItem> processingQueue)
        {
            var nodeFolderTree = await workItem.Entry.GetEntryAsync<TreeEntry>().ConfigureAwait(false);
            ValidateNodeId(workItem.Entry.Name);
            var nodeDataFileFound = ProcessNodeFolderItems(workItem.Entry.Name, nodeFolderTree, workItem.PathStack, processingQueue);
            if (!nodeDataFileFound)
            {
                throw new GitObjectDbValidationException($"Node data folder '{workItem.Entry.Name}.json' could be found in {string.Join('/', workItem.PathStack.Reverse())}.");
            }
        }

        private readonly void ValidateNodeId(string nodeId)
        {
            if (!UniqueId.TryParse(nodeId, out var id))
            {
                throw new GitObjectDbValidationException($"Folder name '{nodeId}' could not be parsed as a valid {nameof(UniqueId)}.");
            }
        }

        private bool ProcessNodeFolderItems(string id, TreeEntry nodeFolderTree, Stack<string> path, Queue<ValidationWorkItem> processingQueue)
        {
            var result = false;
            foreach (var item in nodeFolderTree.Children)
            {
                var newPath = new Stack<string>(path.Reverse());
                newPath.Push(item.Name);

                switch (item.Mode.Type)
                {
                    case ObjectType.Tree when FileSystemStorage.IsResourceName(item.Name):
                        var resourceWorkItem = new ValidationWorkItem(
                            Entry: item,
                            PathStack: newPath,
                            WorkType: ValidationWorkType.Resources);
                        processingQueue.Enqueue(resourceWorkItem);
                        break;
                    case ObjectType.GitLink when FileSystemStorage.IsResourceName(item.Name):
                        ValidateLinkResources(newPath);
                        break;
                    case ObjectType.Tree:
                        var nodeCollectionWorkItem = new ValidationWorkItem(
                            Entry: item,
                            PathStack: newPath,
                            WorkType: ValidationWorkType.NodeCollection);
                        processingQueue.Enqueue(nodeCollectionWorkItem);
                        break;
                    case ObjectType.RegularFile
                        when item.Name.Equals($"{id}.{_serializer.FileExtension}", StringComparison.Ordinal):
                        result = true;
                        break;
                    case ObjectType.RegularFile:
                        ValidateBlobIsExistingNodePropertyValue(item, nodeFolderTree);
                        break;
                    case ObjectType.GitLink:
                        throw new GitObjectDbValidationException($"A link folder is only valid as a resource. " +
                            $"Git link '{item.Name}' was not expected.");
                    default:
                        throw new NotSupportedException($"{item.Mode.Type} is not supported.");
                }
            }
            return result;
        }

        [ExcludeFromCodeCoverage]
        private static void ThrowIfDifferentNodeFolderValues(IEnumerable<IGrouping<bool, NodeTypeDescription>> useNodeFolder)
        {
            if (useNodeFolder.Count() > 1)
            {
                throw new GitObjectDbValidationException($"A model containing several types with different values " +
                    $"for {nameof(GitFolderAttribute.UseNodeFolders)} is not supported.");
            }
        }

        private static async Task ProcessResourcesAsync(ValidationWorkItem workItem)
        {
            var gitPath = string.Join("/", workItem.PathStack.Reverse());
            var traversed = workItem.Entry.TraverseAsync(gitPath, includeSelf: false);
            await foreach (var e in traversed.ConfigureAwait(false))
            {
                if (e.Entry.Mode.Type == ObjectType.GitLink ||
                    (e.Entry.Mode.Type == ObjectType.Tree &&
                    FileSystemStorage.IsResourceName(e.Entry.Name)))
                {
                    ThrowGitLinkOrResourceFolderNotExpected();
                }
            }
        }

        private static void ThrowGitLinkOrResourceFolderNotExpected()
        {
            throw new GitObjectDbValidationException(
                $"A resource folder or link was not expected to be found in a node collection.");
        }

        private void ValidateLinkResources(Stack<string> path)
        {
            var gitPath = string.Join("/", path.Reverse());
            var module = _modules?[gitPath];
            if (module is null)
            {
                throw new GitObjectDbValidationException(
                    $"Linked resource {gitPath} could not be found in .gitmodules.");
            }
        }
    }
}