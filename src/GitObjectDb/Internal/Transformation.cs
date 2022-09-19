using System.Diagnostics;

namespace GitObjectDb.Internal
{
    [DebuggerDisplay("{Message}")]
    internal class Transformation : ITransformation
    {
        internal Transformation(ApplyUpdateTreeDefinition transformation, string message)
        {
            TreeTransformation = transformation;
            Message = message;
        }

        public ApplyUpdateTreeDefinition TreeTransformation { get; }

        public string Message { get; }

        public override string ToString() => Message;
    }
}
