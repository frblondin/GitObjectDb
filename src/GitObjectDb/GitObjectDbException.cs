using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb;

/// <summary>
/// The exception that is thrown when an error occurs during application execution.
/// </summary>
public class GitObjectDbException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitObjectDbException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
    public GitObjectDbException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
