using LibGit2Sharp;
using System;

namespace GitObjectDb.Internal.Commands
{
    internal interface ICommitCommand
    {
        Commit Commit(IConnectionInternal connection, TransformationComposer transformationComposer, string message, Signature author, Signature committer, bool amendPreviousCommit = false, Commit? mergeParent = null, Action<ITransformation>? beforeProcessing = null);
    }
}