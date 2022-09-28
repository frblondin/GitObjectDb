using LibGit2Sharp;

namespace GitObjectDb.Api.GraphQL.Model;

/// <summary>Represents changes that occurred to a <see cref="Node"/>.</summary>
/// <typeparam name="TNode">The type of the <see cref="Node"/>.</typeparam>
public class DeltaDto<TNode>
    where TNode : Node
{
    /// <summary>Initializes a new instance of the <see cref="DeltaDto{TNodeDto}"/> class.</summary>
    /// <param name="old">The old state of the node.</param>
    /// <param name="new">The new state of the node.</param>
    /// <param name="commitId">The latest commit id.</param>
    /// <param name="deleted">Whether the node has been deleted.</param>
    public DeltaDto(TNode? old, TNode? @new, ObjectId commitId, bool deleted)
    {
        Old = old;
        New = @new;
        UpdatedAt = commitId;
        Deleted = deleted;
    }

    /// <summary>Gets the old state of the node.</summary>
    public TNode? Old { get; }

    /// <summary>Gets the new state of the node.</summary>
    public TNode? New { get; }

    /// <summary>Gets the latest commit id.</summary>
    public ObjectId UpdatedAt { get; }

    /// <summary>Gets a value indicating whether the node has been deleted.</summary>
    public bool Deleted { get; }
}
