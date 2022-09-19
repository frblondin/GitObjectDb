using System;
using System.Text.Json.Serialization;
using System.Threading;

namespace GitObjectDb.Serialization.Json;

internal class NodeReferenceHandler : ReferenceHandler
{
    internal static AsyncLocal<Func<DataPath, ITreeItem>?> NodeAccessor { get; } =
        new AsyncLocal<Func<DataPath, ITreeItem>?>();

    public override ReferenceResolver CreateResolver()
    {
        return new NodeReferenceResolver(NodeAccessor.Value);
    }
}
