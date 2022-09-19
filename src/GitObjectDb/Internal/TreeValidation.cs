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
    public void Validate(Tree tree, IDataModel model, INodeSerializer serializer)
    {
        new TreeValidationVisitor(tree, model, serializer).Validate();
    }

    private struct TreeValidationVisitor
    {
        private readonly Tree _tree;
        private readonly IDataModel _model;
        private readonly INodeSerializer _serializer;
        private readonly ModuleCommands _modules;

        private readonly ISet<UniqueId> _identifiers = new HashSet<UniqueId>();

        public TreeValidationVisitor(Tree tree, IDataModel model, INodeSerializer serializer)
        {
            _tree = tree;
            _model = model;
            _serializer = serializer;
            _modules = new ModuleCommands(_tree);
        }

        public void Validate()
        {
            var path = new Stack<string>();
            foreach (var item in _tree.Where(i => i.TargetType == TreeEntryTargetType.Tree))
            {
                path.Push(item.Name);
                ValidateNodeCollection(item, path);
                path.Pop();
            }
        }

        private void ValidateNodeCollection(TreeEntry entry, Stack<string> path)
        {
            var types = _model.GetTypesMatchingFolderName(entry.Name);
            if (!types.Any())
            {
                throw new GitObjectDbValidationException($"No type matching folder name '{entry.Name}' could be found.");
            }
            var useNodeFolder = types.GroupBy(t => t.UseNodeFolders);
            ThrowIfDifferentNodeFolderValues(useNodeFolder);
            ValidateNodeCollectionChildren(entry, useNodeFolder.Single().Key, path);
        }

        private void ValidateNodeCollectionChildren(TreeEntry entry,
                                                    bool useNodeFolder,
                                                    Stack<string> path)
        {
            if (useNodeFolder)
            {
                ValidateNodeCollectionChildrenUsingNodeFolder(entry, path);
            }
            else
            {
                ValidateNodeCollectionChildrenNotUsingNodeFolder(entry, path);
            }
        }

        private void ValidateNodeCollectionChildrenNotUsingNodeFolder(TreeEntry entry,
                                                                      Stack<string> path)
        {
            foreach (var item in entry.Target.Peel<Tree>())
            {
                path.Push(item.Name);
                switch (item.TargetType)
                {
                    case TreeEntryTargetType.Blob when item.Name.EndsWith($".{_serializer.FileExtension}", StringComparison.Ordinal):
                        var nodeId = Path.GetFileNameWithoutExtension(item.Name);
                        ValidateNodeId(nodeId);
                        break;
                    case TreeEntryTargetType.Blob:
                        throw new GitObjectDbValidationException($"A node collection with {nameof(GitFolderAttribute.UseNodeFolders)} = false " +
                            $"should only contain nodes of type '<ParentNodeId>.json'. Blob entry {item.Name}' was not expected.");
                    case TreeEntryTargetType.Tree:
                    case TreeEntryTargetType.GitLink:
                        throw new GitObjectDbValidationException($"A tree or link was not expected in a node collection that does " +
                            $"not use {nameof(GitFolderAttribute.UseNodeFolders)}.");
                    default:
                        throw new NotSupportedException($"{item.TargetType} is not supported.");
                }
                path.Pop();
            }
        }

        private void ValidateNodeCollectionChildrenUsingNodeFolder(TreeEntry entry,
                                                                   Stack<string> path)
        {
            foreach (var item in entry.Target.Peel<Tree>())
            {
                path.Push(item.Name);
                switch (item.TargetType)
                {
                    case TreeEntryTargetType.Tree when !FileSystemStorage.IsResourceName(item.Name):
                        ValidateNodeFolder(item, path);
                        break;
                    case TreeEntryTargetType.Blob:
                        throw new GitObjectDbValidationException($"A blob was not expected to be found in a node collection " +
                            $"using {nameof(GitFolderAttribute.UseNodeFolders)}.");
                    case TreeEntryTargetType.Tree when item.Name == FileSystemStorage.ResourceFolder:
                    case TreeEntryTargetType.GitLink:
                        ThrowGitLinkOrResourceFolderNotExpected();
                        return;
                    default:
                        throw new NotSupportedException($"{item.TargetType} is not supported.");
                }
                path.Pop();
            }
        }

        private void ValidateNodeFolder(TreeEntry nodeFolder, Stack<string> path)
        {
            var nodeFolderTree = nodeFolder.Target.Peel<Tree>();
            ValidateNodeId(nodeFolder.Name);
            var nodeDataFileFound = ValidateNodeFolderItems(nodeFolder.Name, nodeFolderTree, path);
            if (!nodeDataFileFound)
            {
                throw new GitObjectDbValidationException($"Node data folder '{nodeFolder.Name}.json' could be found in {nodeFolder.Path}.");
            }
        }

        private void ValidateNodeId(string nodeId)
        {
            if (!UniqueId.TryParse(nodeId, out var id))
            {
                throw new GitObjectDbValidationException($"Folder name '{nodeId}' could not be parsed as a valid {nameof(UniqueId)}.");
            }
            if (_identifiers.Contains(id))
            {
                throw new GitObjectDbValidationException($"Node id '{nodeId}' exists for multiple nodes.");
            }
            _identifiers.Add(id);
        }

        private bool ValidateNodeFolderItems(string id,
                                             Tree nodeFolderTree,
                                             Stack<string> path)
        {
            var result = false;
            foreach (var item in nodeFolderTree)
            {
                path.Push(item.Name);
                switch (item.TargetType)
                {
                    case TreeEntryTargetType.Tree when FileSystemStorage.IsResourceName(item.Name):
                        ValidateResources(item, path);
                        break;
                    case TreeEntryTargetType.GitLink when FileSystemStorage.IsResourceName(item.Name):
                        ValidateLinkResources(path);
                        break;
                    case TreeEntryTargetType.Tree:
                        ValidateNodeCollection(item, path);
                        break;
                    case TreeEntryTargetType.Blob when item.Name.Equals($"{id}.json", StringComparison.Ordinal):
                        result = true;
                        break;
                    case TreeEntryTargetType.Blob:
                        throw new GitObjectDbValidationException($"A node folder should only contain a file named '<ParentNodeId>.json'. " +
                            $"Blob entry '{item.Name}' was not expected.");
                    case TreeEntryTargetType.GitLink:
                        throw new GitObjectDbValidationException($"A link folder is only valid as a resource. " +
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
                throw new GitObjectDbValidationException($"A model containing several types with different values " +
                    $"for {nameof(GitFolderAttribute.UseNodeFolders)} is not supported.");
            }
        }

        private void ValidateResources(TreeEntry entry, Stack<string> path)
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

            ThrowIfMixingEmbeddedAndLinkedResources(path);
        }

        private static void ThrowGitLinkOrResourceFolderNotExpected()
        {
            throw new GitObjectDbValidationException(
                $"A resource folder or link was not expected to be found in a node collection.");
        }

        private void ThrowIfMixingEmbeddedAndLinkedResources(Stack<string> path)
        {
            var gitPath = string.Join("/", path.Reverse());
            var module = _modules[gitPath];
            if (module is not null)
            {
                throw new GitObjectDbValidationException(
                    $"Cannot mix embedded and linked resources for the same node {gitPath}");
            }
        }

        private void ValidateLinkResources(Stack<string> path)
        {
            var gitPath = string.Join("/", path.Reverse());
            var module = _modules[gitPath];
            if (module is null)
            {
                throw new GitObjectDbValidationException(
                    $"Linked resource {gitPath} could not be found in .gitmodules.");
            }
        }
    }
}
