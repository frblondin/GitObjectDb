namespace GitObjectDb.Model;

/// <summary>The status of what happened as a result of a cherry-pick.</summary>
public enum CherryPickStatus
{
    /// <summary>The commit was successfully cherry picked.</summary>
    CherryPicked,

    /// <summary>The cherry pick resulted in conflicts.</summary>
    Conflicts,
}
