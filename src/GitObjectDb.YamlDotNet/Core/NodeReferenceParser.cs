using GitObjectDb.YamlDotNet.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using YamlDotNet.Core;

namespace GitObjectDb.YamlDotNet.Core;
internal class NodeReferenceParser : Parser, IDisposable
{
    private static readonly AsyncLocal<NodeReferenceParser> _current = new();
    private readonly NodeReferenceParser? _parent;
    private bool _isDisposed;

    public NodeReferenceParser(TextReader input, DataPath path, INodeSerializer.ItemLoader referenceResolver)
        : base(input)
    {
        Path = path;
        ReferenceResolver = referenceResolver;
        _parent = _current.Value;
        _current.Value = this;
        Nodes = _parent?.Nodes ?? new();
        UpdatedDeprecatedNodes = _parent?.UpdatedDeprecatedNodes ?? new();
        ReferencesToBeResolved = _parent?.ReferencesToBeResolved ?? new();
    }

    internal bool IsRoot => _parent is null;

    internal List<Node> Nodes { get; }

    internal List<KeyValuePair<Node, Node>> UpdatedDeprecatedNodes { get; }

    internal List<NodeReference> ReferencesToBeResolved { get; }

    public DataPath Path { get; }

    public INodeSerializer.ItemLoader ReferenceResolver { get; }

    public static NodeReferenceParser CurrentInstance =>
        _current.Value ??
        throw new NotSupportedException($"No {nameof(NodeReferenceParser)} instance is in use.");

    public static NodeReferenceParser? TryGetCurrentInstance() => _current.Value;

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _current.Value = _parent!;
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
