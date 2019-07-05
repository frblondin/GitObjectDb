using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb
{
    /// <summary>
    /// Represents a validation rule.
    /// </summary>
    [Flags]
    public enum ValidationRules
    {
#pragma warning disable CA1008 // Enums should have zero value
#pragma warning disable S2346 // Flags enumerations zero-value members should be named "None"
        /// <summary>
        /// No specific rule.
        /// </summary>
        All = 0,
#pragma warning restore S2346 // Flags enumerations zero-value members should be named "None"
#pragma warning restore CA1008 // Enums should have zero value

        /// <summary>
        /// The dependency rule.
        /// </summary>
        Dependency = 1,
    }
}
