using LibGit2Sharp;
using System;

namespace GitObjectDb;

/// <summary>A set of methods for instances of <see cref="IRepository"/>.</summary>
internal static class IRepositoryExtensions
{
    /// <summary>Builds the commit log message.</summary>
    /// <param name="commit">The commit.</param>
    /// <param name="amendPreviousCommit">if set to <c>true</c> [amend previous commit].</param>
    /// <param name="isHeadOrphaned">if set to <c>true</c> [is head orphaned].</param>
    /// <param name="isMergeCommit">if set to <c>true</c> [is merge commit].</param>
    /// <returns>The commit log message.</returns>
    internal static string BuildCommitLogMessage(this Commit commit,
                                                 bool amendPreviousCommit,
                                                 bool isHeadOrphaned,
                                                 bool isMergeCommit)
    {
        string kind;
        if (isHeadOrphaned)
        {
            kind = " (initial)";
        }
        else if (amendPreviousCommit)
        {
            kind = " (amend)";
        }
        else if (isMergeCommit)
        {
            kind = " (merge)";
        }
        else
        {
            kind = string.Empty;
        }

        return $"commit{kind}: {commit.MessageShort}";
    }

#pragma warning disable SA1611 // Element parameters should be documented
    /// <summary>Updates the head and terminal reference.</summary>
    internal static void UpdateHeadAndTerminalReference(this IRepository repository,
                                                        Commit commit,
                                                        string reflogMessage)
    {
        repository.UpdateTerminalReference(repository.Refs.Head, commit, reflogMessage);
    }

    /// <summary>Updates the reference.</summary>
    internal static void UpdateTerminalReference(this IRepository repository,
                                                 Reference reference,
                                                 Commit commit,
                                                 string reflogMessage)
    {
        var localReference = reference;
        while (true)
        {
            switch (localReference)
            {
                case DirectReference direct:
                    repository.Refs.UpdateTarget(direct, commit.Id, reflogMessage);
                    return;
                case SymbolicReference symRef:
                    localReference = symRef.Target;
                    if (localReference == null)
                    {
                        repository.Refs.Add(symRef.TargetIdentifier, commit.Id, reflogMessage);
                        return;
                    }
                    break;
                default:
                    var message = $"The reference type {localReference?.GetType().ToString() ?? "null"} is not supported.";
                    throw new NotSupportedException(message);
            }
        }
    }
#pragma warning restore SA1611
}
