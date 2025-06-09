using System.Diagnostics.CodeAnalysis;

namespace GitObjectDb.Comparison;

public abstract partial class Change
{
    /// <summary>
    /// Contains the details about the changes made to a <see cref="Resource"/>.
    /// </summary>
    /// <seealso cref="GitObjectDb.Comparison.Change" />
    public class ResourceChange : Change
    {
        internal ResourceChange(GitDotNet.Change changes, Resource? old, Resource? @new, ChangeStatus status)
            : base(old, @new, status)
        {
            Changes = changes;
        }

        /// <summary>Gets the old resource.</summary>
        [ExcludeFromCodeCoverage]
        public new Resource? Old => base.Old as Resource;

        /// <summary>Gets the new resource.</summary>
        [ExcludeFromCodeCoverage]
        public new Resource? New => base.New as Resource;

        /// <summary>Gets the changes between the two resources.</summary>
        [ExcludeFromCodeCoverage]
        public GitDotNet.Change Changes { get; }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage]
        public override string Message => Status.ToString();
    }
}
