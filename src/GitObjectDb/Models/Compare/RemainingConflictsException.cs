using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models.Compare
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
        public RemainingConflictsException(IEnumerable<ObjectRepositoryChunkChange> conflicts, Exception innerException = null)
            : base(CreateMessage(conflicts), innerException)
        {
            Conflicts = conflicts ?? throw new ArgumentNullException(nameof(conflicts));
        }

        /// <summary>
        /// Gets the conflicts.
        /// </summary>
        public IEnumerable<ObjectRepositoryChunkChange> Conflicts { get; }

        private static string CreateMessage(IEnumerable<ObjectRepositoryChunkChange> conflicts)
        {
            if (conflicts == null)
            {
                throw new ArgumentNullException(nameof(conflicts));
            }
            return $"Remaining conflicts were not resolved:\n{string.Join("\n", conflicts.Select(c => $"Property {c.Property.Name} is in conflict state for '{c.Path}.'"))}";
        }
    }
}
