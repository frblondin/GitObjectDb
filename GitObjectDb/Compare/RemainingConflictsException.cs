using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Compare
{
    /// <summary>
    /// The exception that is thrown when remaining conflicts were not resolved.
    /// </summary>
    public class RemainingConflictsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemainingConflictsException"/> class.
        /// </summary>
        /// <param name="conflicts">The conflicts.</param>
        /// <param name="innerException">The inner exception.</param>
        public RemainingConflictsException(IEnumerable<MetadataTreeMergeChunkChange> conflicts, Exception innerException = null)
            : base(CreateMessage(conflicts), innerException)
        {
            Conflicts = conflicts ?? throw new ArgumentNullException(nameof(conflicts));
        }

        /// <summary>
        /// Gets the conflicts.
        /// </summary>
        public IEnumerable<MetadataTreeMergeChunkChange> Conflicts { get; }

        static string CreateMessage(IEnumerable<MetadataTreeMergeChunkChange> conflicts)
        {
            if (conflicts == null)
            {
                throw new ArgumentNullException(nameof(conflicts));
            }
            return $"Remaining conflicts were not resolved:\n{string.Join("\n", conflicts.Select(c => $"Property {c.Property.Name} is in conflict state for '{c.Path}.'"))}";
        }
    }
}
