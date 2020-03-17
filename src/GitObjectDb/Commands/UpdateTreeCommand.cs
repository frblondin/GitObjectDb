using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Commands
{
    internal class UpdateTreeCommand
    {
        private readonly INodeSerializer _serializer;

        public UpdateTreeCommand(INodeSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        internal ApplyUpdateTreeDefinition CreateOrUpdate(ITreeItem item) =>
            (database, definition, reference) =>
            {
                switch (item)
                {
                    case Node node:
                        CreateOrUpdateJsonBlob(node, database, definition);

                        // CreateOrUpdateOrDeleteResourceBlobs(node, database, definition, reference);
                        break;
                    case Resource resource:
                        CreateOrUpdateResourceBlob(database, definition, resource);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            };

        internal ApplyUpdateTreeDefinition Delete(ITreeItem item) =>
            (_, definition, __) =>
            {
                // For nodes, delete whole folder containing node and nested entries
                // For resources, only deleted resource
                definition.Remove(item is Node ? item.Path.FolderPath : item.Path.FilePath);
            };

        private void CreateOrUpdateJsonBlob(Node node, ObjectDatabase database, TreeDefinition definition)
        {
            using var stream = _serializer.Serialize(node);
            var blob = database.CreateBlob(stream);
            definition.Add(node.Path.FilePath, blob, Mode.NonExecutableFile);
        }

        private static void CreateOrUpdateResourceBlob(ObjectDatabase database, TreeDefinition definition, Resource resource)
        {
            var stream = resource.GetContentStream();
            var blob = database.CreateBlob(stream);
            definition.Add(resource.Path.FilePath, blob, Mode.NonExecutableFile);
        }
    }
}
