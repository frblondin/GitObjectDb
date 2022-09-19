using LibGit2Sharp;

namespace GitObjectDb.Api.Model;

/// <summary>Represents changes that occurred to a <see cref="Node"/>.</summary>
/// <typeparam name="TNodeDto">The type of the data transfer object.</typeparam>
public class DeltaDto<TNodeDto>
{
    /// <summary>Initializes a new instance of the <see cref="DeltaDto{TNodeDto}"/> class.</summary>
    /// <param name="old">The old state of the node.</param>
    /// <param name="new">The new state of the node.</param>
    /// <param name="commitId">The latest commit id.</param>
    /// <param name="deleted">Whether the node has been deleted.</param>
    public DeltaDto(TNodeDto? old, TNodeDto? @new, ObjectId commitId, bool deleted)
    {
        Old = old;
        New = @new;
        UpdatedAt = commitId.Sha;
        Deleted = deleted;
    }

    /// <summary>Gets the old state of the node.</summary>
    public TNodeDto? Old { get; }

    /// <summary>Gets the new state of the node.</summary>
    public TNodeDto? New { get; }

    /// <summary>Gets the latest commit id.</summary>
    public string UpdatedAt { get; }

    /// <summary>Gets a value indicating whether the node has been deleted.</summary>
    public bool Deleted { get; }
}
