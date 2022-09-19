using LibGit2Sharp;
using System;

namespace GitObjectDb.Internal.Commands;

internal interface ICommitCommand
{
    Commit Commit(IConnection connection,
                  TransformationComposer transformationComposer,
                  CommitDescription description,
                  Action<ITransformation>? beforeProcessing = null);
}
