using LibGit2Sharp;
using System;

namespace GitObjectDb;

/// <summary>A set of methods for instances of <see cref="IRepository"/>.</summary>
internal static partial class IRepositoryExtensions
{
    /// <summary>Builds the commit log message.</summary>
    /// <param name="commit">The commit.</param>
    /// <param name="amendPreviousCommit">if set to <c>true</c> [amend previous commit].</param>
    /// <param name="isHeadOrphaned">if set to <c>true</c> [is head orphaned].</param>
    /// <param name="isMergeCommit">if set to <c>true</c> [is merge commit].</param>
    /// <returns>The commit log message.</returns>
    internal static string BuildCommitLogMessage(this Commit commit, bool amendPreviousCommit, bool isHeadOrphaned, bool isMergeCommit)
    {
        var kind = string.Empty;
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

        return $"commit{kind}: {commit.MessageShort}";
    }

#pragma warning disable SA1611 // Element parameters should be documented
    /// <summary>Updates the head and terminal reference.</summary>
    internal static void UpdateHeadAndTerminalReference(this IRepository repository, Commit commit, string reflogMessage)
    {
        repository.UpdateTerminalReference(repository.Refs.Head, commit, reflogMessage);
    }

    /// <summary>Updates the reference.</summary>
    internal static void UpdateTerminalReference(this IRepository repository, Reference reference, Commit commit, string reflogMessage)
    {
        while (true)
        {
            switch (reference)
            {
                case DirectReference direct:
                    repository.Refs.UpdateTarget(direct, commit.Id, reflogMessage);
                    return;
                case SymbolicReference symRef:
                    reference = symRef.Target;
                    if (reference == null)
                    {
                        repository.Refs.Add(symRef.TargetIdentifier, commit.Id, reflogMessage);
                        return;
                    }
                    break;
                default:
                    throw new NotSupportedException($"The reference type {reference?.GetType().ToString() ?? "null"} is not supported.");
            }
        }
    }
#pragma warning restore SA1611
}
