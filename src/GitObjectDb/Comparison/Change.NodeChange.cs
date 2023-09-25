using ObjectsComparer;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace GitObjectDb.Comparison;

public abstract partial class Change
{
    /// <summary>Contains the details about the changes made to a <see cref="Node"/>.</summary>
    /// <seealso cref="GitObjectDb.Comparison.Change" />
    public class NodeChange : Change
    {
        internal NodeChange(Node? old, Node? @new, ChangeStatus status, IEnumerable<Difference>? differences = null)
            : base(old, @new, status)
        {
            Differences = differences?.ToImmutableList() ?? ImmutableList.Create<Difference>();
            Message = differences != null ? string.Join("\n", differences) : Status.ToString();
        }

        /// <summary>Gets the old node.</summary>
        [ExcludeFromCodeCoverage]
        public new Node? Old => base.Old as Node;

        /// <summary>Gets the new node.</summary>
        [ExcludeFromCodeCoverage]
        public new Node? New => base.New as Node;

        /// <summary>Gets the differences.</summary>
        [ExcludeFromCodeCoverage]
        public IImmutableList<Difference> Differences { get; }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override string Message { get; }
    }
}
