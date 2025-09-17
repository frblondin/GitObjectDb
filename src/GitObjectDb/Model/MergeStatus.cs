namespace GitObjectDb.Model;

/// <summary>The status of what happened as a result of a merge.</summary>
public enum MergeStatus
{
    /// <summary>Merge was up-to-date.</summary>
    UpToDate,

    /// <summary>Fast-forward merge.</summary>
    FastForward,

    /// <summary>Non-fast-forward merge.</summary>
    NonFastForward,

    /// <summary>Merge resulted in conflicts.</summary>
    Conflicts,
}
