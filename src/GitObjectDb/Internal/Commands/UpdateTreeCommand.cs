using GitObjectDb.Serialization;
using LibGit2Sharp;
using System;

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
                        CreateOrUpdateJsonBlob(node, database, definition);
                        break;
                    case Resource resource:
                        CreateOrUpdateResourceBlob(database, definition, resource);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            };

        internal static ApplyUpdateTreeDefinition Delete(ITreeItem item) =>
            (_, definition, __) =>
            {
                var path = item.Path;
                if (path is null)
                {
                    throw new InvalidOperationException("Path should not be null.");
                }

                // For nodes, delete whole folder containing node and nested entries
                // For resources, only deleted resource
                definition.Remove(item is Node ? path.FolderPath : path.FilePath);
            };

        internal static ApplyUpdateTreeDefinition Delete(DataPath path) =>
            (_, definition, __) =>
            {
                // For nodes, delete whole folder containing node and nested entries
                // For resources, only deleted resource
                definition.Remove(path.FileName == FileSystemStorage.DataFile ? path.FolderPath : path.FilePath);
            };

        private void CreateOrUpdateJsonBlob(Node node, ObjectDatabase database, TreeDefinition definition)
        {
            using var stream = _serializer.Serialize(node);
            var blob = database.CreateBlob(stream);
            var path = node.Path;
            if (path is null)
            {
                throw new InvalidOperationException("Path should not be null.");
            }
            definition.Add(path.FilePath, blob, Mode.NonExecutableFile);
        }

        private static void CreateOrUpdateResourceBlob(ObjectDatabase database, TreeDefinition definition, Resource resource)
        {
            var stream = resource.GetContentStream();
            var blob = database.CreateBlob(stream);
            definition.Add(resource.Path.FilePath, blob, Mode.NonExecutableFile);
        }
    }
}
