namespace GitObjectDb
{
    /// <summary>Type of commit to be used while modifying the repository.</summary>
    public enum CommitCommandType
    {
        /// <summary>Choose automatically the most appropriate method.</summary>
        Auto = 0,

        /// <summary>
        /// Use git <see href="fast-import">https://git-scm.com/docs/git-fast-import</see> method
        /// to submit changes to the repository.<BR/>
        /// This method requires git CLI to be installed on the host.
        /// </summary>
        FastImport,

        /// <summary>Standard commit where transformations are transformed into tree edits.</summary>
        Normal,
    }
}
