using System;
using System.Diagnostics;

namespace GitObjectDb.Internal;

[DebuggerDisplay("{Message}")]
internal class Transformation : ITransformationInternal
{
    internal Transformation(DataPath path,
                            TreeItem? item,
                            Delegate action,
                            string message)
    {
        Path = path;
        Item = item;
        Action = action;
        Message = message;
    }

    public DataPath Path { get; }

    public TreeItem? Item { get; }

    public Delegate Action { get; }

    public string Message { get; }

    public override string ToString() => Message;
}
