using LibGit2Sharp;

namespace GitObjectDb.Api.GraphQL.Model;

/// <summary>Represents changes that occurred to a <see cref="Node"/>.</summary>
/// <typeparam name="TNode">The type of the <see cref="Node"/>.</typeparam>
/// <remarks>Initializes a new instance of the <see cref="DeltaDto{TNodeDto}"/> class.</remarks>
/// <param name="Old">The old state of the node.</param>
/// <param name="New">The new state of the node.</param>
/// <param name="UpdatedAt">The latest commit id.</param>
/// <param name="Deleted">Whether the node has been deleted.</param>
#pragma warning disable SA1402 // File may only contain a single type
public record DeltaDto<TNode>(TNode? Old, TNode? New, ObjectId UpdatedAt, bool Deleted)
    : DeltaDto(UpdatedAt, Deleted)
    where TNode : Node;

/// <summary>Represents changes that occurred to a <see cref="Node"/>.</summary>
/// <remarks>Initializes a new instance of the <see cref="DeltaDto"/> class.</remarks>
/// <param name="CommitId">The latest commit id.</param>
/// <param name="Deleted">Whether the node has been deleted.</param>
public abstract record DeltaDto(ObjectId CommitId, bool Deleted);