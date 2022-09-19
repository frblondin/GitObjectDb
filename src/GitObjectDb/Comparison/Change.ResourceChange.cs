using LibGit2Sharp;
using System.Diagnostics.CodeAnalysis;

namespace GitObjectDb.Comparison
{
    public abstract partial class Change
    {
        /// <summary>
        /// Contains the details about the changes made to a <see cref="Resource"/>.
        /// </summary>
        /// <seealso cref="GitObjectDb.Comparison.Change" />
#pragma warning disable CA1034 // Nested types should not be visible
        public class ResourceChange : Change
        {
            internal ResourceChange(ContentChanges changes, Resource? old, Resource? @new, ChangeStatus status)
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
            public ContentChanges Changes { get; }

            /// <inheritdoc/>
            [ExcludeFromCodeCoverage]
            public override string Message =>
                Changes != null ?
                $@"{{+{Changes.LinesAdded}, -{Changes.LinesDeleted}}}" :
                Status.ToString();
        }
    }
}
