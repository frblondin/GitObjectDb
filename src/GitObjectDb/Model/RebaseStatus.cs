namespace GitObjectDb.Model;

/// <summary>The status of the rebase.</summary>
public enum RebaseStatus
{
    /// <summary>The rebase operation was run to completion.</summary>
    Complete,

    /// <summary>The rebase operation hit a conflict and stopped.</summary>
    Conflicts,

    /// <summary>The rebase operation has hit a user requested stop point (edit, reword, ect.)</summary>
    Stop,
}