using LibGit2Sharp;
using System;
using System.Diagnostics;

namespace GitObjectDb.Comparison
{
    /// <summary>Contains the set of differences between two nodes.</summary>
    [DebuggerDisplay("Path = {Path,nq}, Message = {Message,nq}")]
    public abstract partial class Change
    {
        internal Change(ITreeItem? old, ITreeItem? @new, ChangeStatus status)
        {
            Old = old;
            New = @new;
            Status = status;
        }

        /// <summary>Gets the old item.</summary>
        public ITreeItem? Old { get; }

        /// <summary>Gets the new item.</summary>
        public ITreeItem? New { get; }

        /// <summary>Gets the change status.</summary>
        public ChangeStatus Status { get; }

        /// <summary>Gets the message.</summary>
        public abstract string Message { get; }

        /// <summary>Gets the item path.</summary>
        public DataPath Path => (New ?? Old)?.Path ?? throw new InvalidOperationException();

        /// <inheritdoc/>
        public override string ToString() =>
            Message;

        internal static Change? Create(ContentChanges changes, ITreeItem? old, ITreeItem? @new, ChangeStatus status, ComparisonPolicy policy)
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
    }
}
