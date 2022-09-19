using LibGit2Sharp;
using System;
using System.Text.Json.Serialization;
using System.Threading;

namespace GitObjectDb.Serialization.Json;

internal delegate ITreeItem ItemLoader(DataPath path);

internal class NodeReferenceHandler : ReferenceHandler
{
    internal static AsyncLocal<DataContext?> CurrentContext { get; } =
        new AsyncLocal<DataContext?>();

    public override ReferenceResolver CreateResolver()
    {
        var context = CurrentContext.Value;
        return new NodeReferenceResolver(context);
    }

    internal class DataContext
    {
        public DataContext(ItemLoader accessor, ObjectId treeId)
        {
            Accessor = accessor;
            TreeId = treeId;
        }

        internal ItemLoader Accessor { get; }

        internal ObjectId TreeId { get; }
    }
}
