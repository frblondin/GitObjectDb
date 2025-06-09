using GitDotNet;

namespace GitObjectDb;

/// <summary>Provides a description of relevant data for a git commit.</summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CommitDescription"/> class.
/// </remarks>
/// <param name="message">The commit message.</param>
/// <param name="author">The commit author.</param>
/// <param name="committer">The commit committer.</param>
/// <param name="amendPreviousCommit">Whether previous commit message should be amended.</param>
/// <param name="mergeParent">The merge commit, if any.</param>
public class CommitDescription(string message,
    Signature? author,
    Signature? committer,
    bool amendPreviousCommit = false,
    CommitEntry? mergeParent = null)
{
    /// <summary>Gets the commit message.</summary>
    public string Message { get; } = message;

    /// <summary>Gets the commit author.</summary>
    public Signature? Author { get; } = author;

    /// <summary>Gets the committer.</summary>
    public Signature? Committer { get; } = committer;

    /// <summary>Gets a value indicating whether the previous commit should ba amended.</summary>
    public bool AmendPreviousCommit { get; } = amendPreviousCommit;

    /// <summary>Gets the parent merge commit, if any.</summary>
    public CommitEntry? MergeParent { get; } = mergeParent;
}
