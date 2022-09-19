using System.Collections.Immutable;
using System.Reflection;

namespace GitObjectDb.Comparison
{
    /// <summary>Provides a description of a cherry-pick policy.</summary>
    public record CherryPickPolicy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CherryPickPolicy"/> class.
        /// </summary>
        /// <param name="policy">The comparison policy.</param>
        public CherryPickPolicy(ComparisonPolicy policy)
        {
            ComparisonPolicy = policy;
        }

        /// <summary>
        /// Gets the comparison policy.
        /// </summary>
        public ComparisonPolicy ComparisonPolicy { get; init; }

        /// <summary>
        /// Gets or sets the parent number to consider as mainline, starting from offset 1.
        /// As a merge commit has multiple parents, cherry picking a merge commit will take
        /// only the changes relative to the given parent. The parent to consider changes
        /// based on is called the mainline, and must be specified by its number (i.e. offset).
        /// </summary>
        public int Mainline { get; set; }
    }
}
