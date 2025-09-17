using GitDotNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitObjectDb.Internal.Commands;

internal interface ICommitCommand
{
    Task<CommitEntry> CommitAsync(ChangeComposer composer,
        CommitDescription description,
        Action<ITransformation>? beforeProcessing = null);

    Task<CommitEntry> CommitAsync(IConnection connection,
        string branchName,
        IEnumerable<ApplyUpdate> transformations,
        CommitDescription description,
        CommitEntry predecessor);

    Task<CommitEntry> CommitAsync(IConnection connection,
        Func<GitDotNet.ITransformationComposer, Task<GitDotNet.ITransformationComposer>> transform,
        string branchName,
        List<CommitEntry> parents,
        CommitDescription description);
}
