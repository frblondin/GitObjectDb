using GitObjectDb.Serialization.Json;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Commands
{
    internal static class UpdateTreeCommand
    {
        internal static Action<ObjectDatabase, TreeDefinition> CreateOrUpdate(Node node) =>
            (database, definition) =>
            {
                using var stream = DefaultSerializer.Serialize(new NonScalar(node));
                var blob = database.CreateBlob(stream);
                definition.Add(node.Path.DataPath, blob, Mode.NonExecutableFile);
            };

        internal static Action<ObjectDatabase, TreeDefinition> Delete(Node node) =>
            (_, definition) =>
            {
                definition.Remove(node.Path.FolderPath);
            };
    }
}
