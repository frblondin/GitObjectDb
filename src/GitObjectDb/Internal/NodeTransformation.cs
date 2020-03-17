using GitObjectDb.Commands;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GitObjectDb.Internal
{
    [DebuggerDisplay("{Message}")]
    internal class NodeTransformation : INodeTransformation
    {
        internal NodeTransformation(ApplyUpdateTreeDefinition transformation, string message)
        {
            Transformation = transformation;
            Message = message;
        }

        public ApplyUpdateTreeDefinition Transformation { get; }

        public string Message { get; }
    }
}
