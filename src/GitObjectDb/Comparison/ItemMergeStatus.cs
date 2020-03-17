namespace GitObjectDb.Comparison
{
    /// <summary>The status of what happened as a result of a node merge.</summary>
    public enum ItemMergeStatus
    {
        /// <summary>The node will remain the same after merge.</summary>
        NoChange,

        /// <summary>The node was edited.</summary>
        Edit,

        /// <summary>The node was added.</summary>
        Add,

        /// <summary>The node was deleted.</summary>
        Delete,

        /// <summary>The node was edited in both sides.</summary>
        EditConflict,

        /// <summary>The node was modified on one side whereas tree changes were performed on the other side.</summary>
        TreeConflict,
    }
}
