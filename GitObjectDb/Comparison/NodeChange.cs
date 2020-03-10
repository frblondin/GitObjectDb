using KellermanSoftware.CompareNetObjects;
using System.Collections.Immutable;
using System.Diagnostics;

namespace GitObjectDb.Comparison
{
    /// <summary>Contains the set of differences between two nodes.</summary>
    [DebuggerDisplay("Status = {Status}, Path = {Path}")]
    public sealed class NodeChange
    {
        internal NodeChange(Node old, Node @new, NodeChangeStatus status, ComparisonResult differences = null)
        {
            Old = old;
            New = @new;
            Status = status;
            Differences = differences?.Differences.ToImmutableList();
            Message = differences?.DifferencesString;
        }

        /// <summary>Gets the old node.</summary>
        public Node Old { get; }

        /// <summary>Gets the new node.</summary>
        public Node New { get; }

        /// <summary>Gets the change status.</summary>
        public NodeChangeStatus Status { get; }

        /// <summary>Gets the differences.</summary>
        public IImmutableList<Difference> Differences { get; }

        /// <summary>Gets the message.</summary>
        public string Message { get; }

        /// <summary>Gets the path of the node.</summary>
        public Path Path => (New ?? Old).Path;

        /// <inheritdoc/>
        public override string ToString() => Message;
    }
}
