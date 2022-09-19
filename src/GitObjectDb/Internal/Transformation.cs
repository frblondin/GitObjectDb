using System.Diagnostics;

namespace GitObjectDb.Internal;

[DebuggerDisplay("{Message}")]
internal class Transformation : ITransformation
{
    internal Transformation(ApplyUpdateTreeDefinition transformation, ApplyUpdateFastInsert fastInsertTransformation, string message)
    {
        TreeTransformation = transformation;
        FastInsertTransformation = fastInsertTransformation;
        Message = message;
    }

    public ApplyUpdateTreeDefinition TreeTransformation { get; }

    public ApplyUpdateFastInsert FastInsertTransformation { get; }

    public string Message { get; }

    public override string ToString() => Message;
}
