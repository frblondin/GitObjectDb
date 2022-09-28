using System;

namespace GitObjectDb;

/// <summary>The exception that is thrown when an error occurs during application execution.</summary>
public class GitObjectDbException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="GitObjectDbException"/> class.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception,
    /// or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
    public GitObjectDbException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

#pragma warning disable SA1402 // File may only contain a single type
/// <summary>The exception that is thrown when a validation error occurs during application execution.</summary>
public class GitObjectDbValidationException : GitObjectDbException
{
    /// <summary>Initializes a new instance of the <see cref="GitObjectDbValidationException"/> class.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception,
    /// or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
    public GitObjectDbValidationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>The exception that is thrown when commit id is invalid.</summary>
public class GitObjectDbInvalidCommitException : GitObjectDbException
{
    /// <summary>Initializes a new instance of the <see cref="GitObjectDbInvalidCommitException"/> class.</summary>
    /// <param name="innerException">The exception that is the cause of the current exception,
    /// or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
    public GitObjectDbInvalidCommitException(Exception? innerException = null)
        : base("No valid commit could be found.", innerException)
    {
    }
}

/// <summary>The exception that is thrown when trying to use a branch which doesn't exist.</summary>
public class GitObjectDbNonExistingBranchException : GitObjectDbException
{
    /// <summary>Initializes a new instance of the <see cref="GitObjectDbNonExistingBranchException"/> class.</summary>
    /// <param name="innerException">The exception that is the cause of the current exception,
    /// or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
    public GitObjectDbNonExistingBranchException(Exception? innerException = null)
        : base($"Branch does not exist.", innerException)
    {
    }
}