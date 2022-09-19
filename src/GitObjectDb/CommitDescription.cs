using LibGit2Sharp;

namespace GitObjectDb;

/// <summary>Provides a description of relevant data for a git commit.</summary>
public class CommitDescription
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommitDescription"/> class.
    /// </summary>
    /// <param name="message">The commit message.</param>
    /// <param name="author">The commit author.</param>
    /// <param name="committer">The commit committer.</param>
    /// <param name="amendPreviousCommit">Whether previous commit message should be amended.</param>
    /// <param name="mergeParent">The merge commit, if any.</param>
    public CommitDescription(string message,
                             Signature author,
                             Signature committer,
                             bool amendPreviousCommit = false,
                             Commit? mergeParent = null)
    {
        Message = message;
        Author = author;
        Committer = committer;
        AmendPreviousCommit = amendPreviousCommit;
        MergeParent = mergeParent;
    }

    /// <summary>Gets the commit message.</summary>
    public string Message { get; }

    /// <summary>Gets the commit author.</summary>
    public Signature Author { get; }

    /// <summary>Gets the committer.</summary>
    public Signature Committer { get; }

    /// <summary>Gets a value indicating whether the previous commit should ba amended.</summary>
    public bool AmendPreviousCommit { get; }

    /// <summary>Gets the parent merge commit, if any.</summary>
    public Commit? MergeParent { get; }
}
