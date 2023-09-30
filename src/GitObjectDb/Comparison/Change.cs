using LibGit2Sharp;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace GitObjectDb.Comparison;

/// <summary>Contains the set of differences between two nodes.</summary>
[DebuggerDisplay("Path = {Path,nq}, Message = {Message,nq}")]
public abstract partial class Change
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Change"/> class.
    /// </summary>
    /// <param name="old">The old item.</param>
    /// <param name="new">The new item.</param>
    /// <param name="status">The change status.</param>
    protected Change(TreeItem? old, TreeItem? @new, ChangeStatus status)
    {
        Old = old;
        New = @new;
        Status = status;
    }

    /// <summary>Gets the old item.</summary>
    [ExcludeFromCodeCoverage]
    public TreeItem? Old { get; }

    /// <summary>Gets the new item.</summary>
    [ExcludeFromCodeCoverage]
    public TreeItem? New { get; }

    /// <summary>Gets the change status.</summary>
    [ExcludeFromCodeCoverage]
    public ChangeStatus Status { get; }

    /// <summary>Gets the message.</summary>
    [ExcludeFromCodeCoverage]
    public abstract string Message { get; }

    /// <summary>Gets the item path.</summary>
    [ExcludeFromCodeCoverage]
    public DataPath Path => (New ?? Old)?.Path ?? throw new InvalidOperationException();

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    public override string ToString() => Message;

    internal static Change? Create(ContentChanges changes,
                                   TreeItem? old,
                                   TreeItem? @new,
                                   ChangeStatus status,
                                   ComparisonPolicy policy)
    {
        var oldNode = old as Node;
        var newNode = @new as Node;
        if (oldNode != null || newNode != null)
        {
            var differences = Comparer.CompareInternal(oldNode, newNode, policy);
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

        throw new NotSupportedException();
    }
}
