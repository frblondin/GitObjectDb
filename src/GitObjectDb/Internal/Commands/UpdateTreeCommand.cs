using GitObjectDb.Serialization;
using LibGit2Sharp;
using System;
using System.IO;

namespace GitObjectDb.Internal.Commands
{
    internal class UpdateTreeCommand
    {
        private readonly INodeSerializer _serializer;

        public UpdateTreeCommand(INodeSerializer serializer)
        {
            _serializer = serializer;
        }

        internal ApplyUpdateTreeDefinition CreateOrUpdate(ITreeItem item) =>
            (database, definition, reference) =>
            {
                switch (item)
                {
                    case Node node:
                        CreateOrUpdateNode(node, database, definition);
                        break;
                    case Resource resource:
                        CreateOrUpdateResource(database, definition, resource);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            };

        internal static ApplyUpdateTreeDefinition Delete(ITreeItem item) =>
            (_, definition, __) =>
            {
                var path = item.ThrowIfNoPath();

                // For nodes, delete whole folder containing node and nested entries
                // For resources, only deleted resource
                definition.Remove(item is Node ? path.FolderPath : path.FilePath);
            };

        internal static ApplyUpdateTreeDefinition Delete(DataPath path) =>
            (_, definition, __) =>
            {
                // For nodes, delete whole folder containing node and nested entries
                // For resources, only deleted resource
                definition.Remove(path.IsNode && path.UseNodeFolders ? path.FolderPath : path.FilePath);
            };

        private void CreateOrUpdateNode(Node node, ObjectDatabase database, TreeDefinition definition)
        {
            using var stream = _serializer.Serialize(node);
            var blob = database.CreateBlob(stream);
            var path = node.ThrowIfNoPath();
            definition.Add(path.FilePath, blob, Mode.NonExecutableFile);
        }

        private static void CreateOrUpdateResource(ObjectDatabase database, TreeDefinition definition, Resource resource)
        {
            var stream = resource.Embedded.GetContentStream();
            var blob = database.CreateBlob(stream);
            definition.Add(resource.Path.FilePath, blob, Mode.NonExecutableFile);
        }
    }
}
