using System.Diagnostics;

namespace GitObjectDb.Internal;

[DebuggerDisplay("{Message}")]
internal class Transformation : ITransformationInternal
{
    internal Transformation(DataPath path,
                            ITreeItem? item,
                            ApplyUpdateTreeDefinition transformation,
                            ApplyUpdateFastInsert fastInsertTransformation,
                            string message)
    {
        Path = path;
        Item = item;
        TreeTransformation = transformation;
        FastInsertTransformation = fastInsertTransformation;
        Message = message;
    }

    public DataPath Path { get; }

    public ITreeItem? Item { get; }

    public ApplyUpdateTreeDefinition TreeTransformation { get; }

    public ApplyUpdateFastInsert FastInsertTransformation { get; }

    public string Message { get; }

    public override string ToString() => Message;
}
