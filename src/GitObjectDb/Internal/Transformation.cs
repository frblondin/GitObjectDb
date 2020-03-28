using GitObjectDb.Internal.Commands;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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
    }
}
