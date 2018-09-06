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
        /// <summary>
        /// No specific rule.
        /// </summary>
        None = 0,

        /// <summary>
        /// The dependency rule.
        /// </summary>
        Dependency = 1
    }
}
