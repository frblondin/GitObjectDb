namespace GitObjectDb.Comparison
{
    /// <summary>The status of what happened as a result of a node merge.</summary>
    public enum NodeMergeStatus
    {
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
