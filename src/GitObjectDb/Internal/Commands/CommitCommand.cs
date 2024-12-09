using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Internal.Commands;

internal class CommitCommand : ICommitCommand
{
    private readonly Func<ITreeValidation> _treeValidation;

    public CommitCommand(Func<ITreeValidation> treeValidation)
    {
        _treeValidation = treeValidation;

        GitCliCommand.ThrowIfGitNotInstalled();
    }

    public Commit Commit(TransformationComposer composer,
                         CommitDescription description,
                         Action<ITransformation>? beforeProcessing = null)
    {
        var branch = composer.Connection.Repository.Branches[composer.BranchName];
        var parents = RetrieveParentsOfTheCommitBeingCreated(
            composer.Connection.Repository,
            branch,
            description.AmendPreviousCommit,
            description.MergeParent).ToList();
        return Commit(composer.Connection,
                      info => ApplyTransformations(composer.Connection,
                                                   composer.Transformations.Values,
                                                   branch?.Tip,
                                                   info.Writer,
                                                   info.Index,
                                                   beforeProcessing),
                      composer.BranchName,
                      parents,
                      description);
    }

    public Commit Commit(IConnection connection,
                         string branchName,
                         IEnumerable<Delegate> transformations,
                         CommitDescription description,
                         Commit predecessor,
                         bool updateBranchTip = true)
    {
        var modules = new ModuleCommands(predecessor.Tree);
        var parents = GetParents(description, predecessor);
        return Commit(connection,
                      info =>
                      {
                          foreach (var transformation in transformations)
                          {
                              var action = (ApplyUpdate)transformation;
                              action.Invoke(predecessor.Tree, modules, connection.Serializer, info.Writer, info.Index);
                          }
                      },
                      branchName,
                      parents,
                      description);
    }

    internal static List<Commit> GetParents(CommitDescription description, Commit predecessor)
    {
        var parents = new List<Commit> { predecessor };
        if (description.MergeParent is not null)
        {
            parents.Add(description.MergeParent);
        }

        return parents;
    }

    public Commit Commit(IConnection connection,
                         Action<ImportFileArguments> transform,
                         string branchName,
                         List<Commit> parents,
                         CommitDescription description)
    {
        var importFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var tempBranch = $"refs/fastimport/{UniqueId.CreateNew()}";
        try
        {
            int commitMarkId;
            using (var writer = new StreamWriter(File.OpenWrite(importFile)) { NewLine = "\n" })
            {
                var index = new List<string>();
                transform((writer, index, tempBranch));

                commitMarkId = index.Count + 1;
                WriteFastInsertCommit(tempBranch, parents, writer, description, commitMarkId);
                WriteFastInsertCommitIndex(writer, index);
            }
            return ValidateAndUpdateBranchTip(connection, branchName, description, importFile, parents, commitMarkId);
        }
        finally
        {
            connection.Repository.Branches.Remove(tempBranch);
            connection.Repository.Refs.Remove(tempBranch);
            TryDelete(importFile);
        }
    }

    private static void ApplyTransformations(IConnection connection,
                                             IEnumerable<ITransformation> transformations,
                                             Commit? commit,
                                             StreamWriter writer,
                                             IList<string> commitIndex,
                                             Action<ITransformation>? beforeProcessing = null)
    {
        var modules = new ModuleCommands(commit?.Tree);
        foreach (var transformation in transformations.OfType<ITransformationInternal>())
        {
            beforeProcessing?.Invoke(transformation);
            transformation.Action.Invoke(commit?.Tree, modules, connection.Serializer, writer, commitIndex);
        }

        if (modules.HasAnyChange)
        {
            using var stream = modules.CreateStream();
            GitUpdateCommand.AddBlob(ModuleCommands.ModuleFile, stream, writer, commitIndex);
        }
    }

    private static void WriteFastInsertCommit(string tempBranch,
                                              List<Commit> parents,
                                              TextWriter writer,
                                              CommitDescription description,
                                              int commitMarkId)
    {
        if (parents.Count == 0)
        {
            writer.WriteLine($"reset {tempBranch}");
        }
        writer.WriteLine($"commit {tempBranch}");
        writer.WriteLine($"mark :{commitMarkId}");
        WriteSignature(writer, "author", description.Author);
        WriteSignature(writer, "committer", description.Committer);
        writer.WriteLine($"data {Encoding.UTF8.GetByteCount(description.Message)}");
        writer.WriteLine(description.Message);
        WriteParentCommits(writer, parents);
    }

    private static void WriteSignature(TextWriter writer, string type, Signature signature)
    {
        writer.WriteLine($"{type} {signature.Name} <{signature.Email}> {signature.When.ToUnixTimeSeconds()} {signature.When.Offset.Minutes:+0000;-0000}");
    }

    private static void WriteParentCommits(TextWriter writer, List<Commit> parents)
    {
        if (parents.Count >= 1)
        {
            writer.WriteLine($"from {parents[0].Id}");
            if (parents.Count >= 2)
            {
                writer.WriteLine($"merge {parents[1].Id}");
            }
        }
    }

    private static void WriteFastInsertCommitIndex(TextWriter writer, List<string> index)
    {
        foreach (var item in index)
        {
            writer.WriteLine(item);
        }
    }

    private static Commit SendCommandThroughCli(IConnection connection, string importFile, int commitMarkId)
    {
        var markFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            using var stream = File.OpenRead(importFile);
            GitCliCommand.Execute(connection.Repository.Info.Path,
                                  @$"fast-import --export-marks=""{markFile}""",
                                  stream);
            var linePrefix = $":{commitMarkId} ";
            var line = File.ReadLines(markFile)
                .FirstOrDefault(l => l.StartsWith(linePrefix, StringComparison.Ordinal)) ??
                throw new GitObjectDbException("Could not locate commit id in fast-import mark file.");
            var commitId = line.Substring(linePrefix.Length).Trim();
            return connection.Repository.Lookup(commitId).Peel<Commit>() ??
                throw new GitObjectDbException($"Commit {commitId} could not be found in repository.");
        }
        finally
        {
            TryDelete(markFile);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Ignored
        }
    }

    private Commit ValidateAndUpdateBranchTip(IConnection connection, string branchName, CommitDescription description, string importFile, List<Commit> parents, int commitMarkId)
    {
        var commit = SendCommandThroughCli(connection, importFile, commitMarkId);
        if (parents.Count == 1 && commit.Tree == parents[0].Tree)
        {
            // If no change, do not create an empty commit
            return parents[0];
        }

        var validation = _treeValidation.Invoke();
        validation.Validate(commit.Tree, connection.Model, connection.Serializer);

        var logMessage = commit.BuildCommitLogMessage(description.AmendPreviousCommit,
                                                      parents.Count > 1);
        var reference = connection.Repository.Branches[branchName]?.Reference ??
            connection.Repository.Refs.UpdateTarget("HEAD", $"refs/heads/{branchName}");
        connection.Repository.UpdateBranchTip(reference, commit, logMessage);

        return commit;
    }

    internal static List<Commit> RetrieveParentsOfTheCommitBeingCreated(IRepository repository,
                                                                        Branch? branch,
                                                                        bool amendPreviousCommit,
                                                                        Commit? mergeParent = null)
    {
        if (amendPreviousCommit)
        {
            if (branch is null)
            {
                throw new GitObjectDbNonExistingBranchException();
            }
            return branch.Tip.Parents.ToList();
        }

        var parents = new List<Commit>();
        if (branch?.Tip is not null)
        {
            parents.Add(branch.Tip);
        }

        if (mergeParent != null)
        {
            parents.Add(mergeParent);
        }

        if (repository.Info.CurrentOperation == CurrentOperation.Merge)
        {
            throw new NotSupportedException();
        }

        return parents;
    }

    internal record struct ImportFileArguments(StreamWriter Writer, List<string> Index, string TempBranch)
    {
        public static implicit operator (StreamWriter Writer, List<string> Index, string TempBranch)(ImportFileArguments value)
        {
            return (value.Writer, value.Index, value.TempBranch);
        }

        public static implicit operator ImportFileArguments((StreamWriter Writer, List<string> Index, string TempBranch) value)
        {
            return new ImportFileArguments(value.Writer, value.Index, value.TempBranch);
        }
    }
}
