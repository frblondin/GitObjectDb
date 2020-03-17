using KellermanSoftware.CompareNetObjects;
using LibGit2Sharp;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace GitObjectDb.Comparison
{
    /// <summary>Contains the set of differences between two nodes.</summary>
    [DebuggerDisplay("Path = {Path,nq}, Message = {Message,nq}")]
    public abstract class Change
    {
        internal Change(ITreeItem old, ITreeItem @new, ChangeStatus status)
        {
            Old = old;
            New = @new;
            Status = status;
        }

        /// <summary>Gets the old item.</summary>
        public ITreeItem Old { get; }

        /// <summary>Gets the new item.</summary>
        public ITreeItem New { get; }

        /// <summary>Gets the change status.</summary>
        public ChangeStatus Status { get; }

        /// <summary>Gets the message.</summary>
        public abstract string Message { get; }

        /// <summary>Gets the item path.</summary>
        public DataPath Path => (New ?? Old).Path;

        /// <inheritdoc/>
        public override string ToString() => Message;

        internal static Change Create(ContentChanges changes, ITreeItem old, ITreeItem @new, ChangeStatus status, ComparisonPolicy policy)
        {
            var oldNode = old as Node;
            var newNode = @new as Node;
            if (oldNode != null || newNode != null)
            {
                var differences = Comparer.Compare(oldNode, newNode, policy);
                return differences.AreEqual ?
                    null :
                    new NodeChange(oldNode, newNode, status, differences);
            }

            var oldResource = old as Resource;
            var newResource = @new as Resource;
            if (oldResource != null || newResource != null)
            {
                return new ResourceChange(changes, oldResource, newResource, status);
            }

            throw new NotImplementedException();
        }

        /// <summary>Contains the details about the changes made to a <see cref="Node"/>.</summary>
        /// <seealso cref="GitObjectDb.Comparison.Change" />
        public class NodeChange : Change
        {
            internal NodeChange(Node old, Node @new, ChangeStatus status, ComparisonResult differences = null)
                : base(old, @new, status)
            {
                Differences = differences?.Differences.ToImmutableList();
                Message = differences?.DifferencesString ?? Status.ToString();
            }

            /// <summary>Gets the old node.</summary>
            public new Node Old => (Node)base.Old;

            /// <summary>Gets the new node.</summary>
            public new Node New => (Node)base.New;

            /// <summary>Gets the differences.</summary>
            public IImmutableList<Difference> Differences { get; }

            /// <inheritdoc/>
            public override string Message { get; }
        }

        /// <summary>
        /// Contains the details about the changes made to a <see cref="Resource"/>.
        /// </summary>
        /// <seealso cref="GitObjectDb.Comparison.Change" />
        public class ResourceChange : Change
        {
            internal ResourceChange(ContentChanges changes, Resource old, Resource @new, ChangeStatus status)
                : base(old, @new, status)
            {
                Changes = changes;
            }

            /// <summary>Gets the old resource.</summary>
            public new Resource Old => (Resource)base.Old;

            /// <summary>Gets the new resource.</summary>
            public new Resource New => (Resource)base.New;

            /// <summary>Gets the changes between the two resources.</summary>
            public ContentChanges Changes { get; }

            /// <inheritdoc/>
            public override string Message =>
                Changes != null ?
                $@"{{+{Changes.LinesAdded}, -{Changes.LinesDeleted}}}" :
                Status.ToString();
        }
    }
}
