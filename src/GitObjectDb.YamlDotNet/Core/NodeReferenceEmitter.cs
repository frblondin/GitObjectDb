using System.Collections.Generic;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace GitObjectDb.YamlDotNet.Core;
internal class NodeReferenceEmitter : IEmitter
{
    private readonly IEmitter _inner;
    private readonly HashSet<object> _contexts = new HashSet<object>();

    public NodeReferenceEmitter(TextWriter writer, Node serializedNode)
    {
        _inner = CreateEmitter(writer);
        SerializedNode = serializedNode;
    }

    public Node SerializedNode { get; }

    public bool AlreadySerialized { get; private set; }

    internal static Emitter CreateEmitter(TextWriter writer) => new(writer, new EmitterSettings(
        bestIndent: 2,
        bestWidth: int.MaxValue,
        isCanonical: false,
        maxSimpleKeyLength: 1024,
        skipAnchorName: false));

    public void Emit(ParsingEvent @event) => _inner.Emit(@event);

    public bool ShouldTraverse(Node node, object context)
    {
        if (node != SerializedNode || _contexts.Contains(context))
        {
            return false;
        }
        else
        {
            // Use a bag to keep track of nodes traversed by contexts
            // and avoid circular ref stack overflow
            _contexts.Add(context);
            return true;
        }
    }
}
