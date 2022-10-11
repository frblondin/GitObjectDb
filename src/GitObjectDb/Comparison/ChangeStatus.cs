namespace GitObjectDb.Comparison;

/// <summary>The status of what happened as a result of a node comparison.</summary>
public enum ChangeStatus
{
    /// <summary>The node was edited.</summary>
    Edit,

    /// <summary>The node was added.</summary>
    Add,

    /// <summary>The node was deleted.</summary>
    Delete,

    /// <summary>The node was renamed.</summary>
    Rename,
}
