using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Internal.Commands;

internal class FastImportCommitCommand : ICommitCommand
{
    private readonly Func<ITreeValidation> _treeValidation;

    public FastImportCommitCommand(Func<ITreeValidation> treeValidation)
    {
        _treeValidation = treeValidation;

        GitCliCommand.ThrowIfGitNotInstalled();
    }

    public Commit Commit(IConnection connection,
                         TransformationComposer transformationComposer,
                         CommitDescription description,
                         Action<ITransformation>? beforeProcessing = null)
    {
        var importFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var tempBranch = $"refs/fastimport/{UniqueId.CreateNew()}";
        try
        {
            var parents = CommitCommand.RetrieveParentsOfTheCommitBeingCreated(connection.Repository,
                                                                               description.AmendPreviousCommit,
                                                                               description.MergeParent).ToList();
            int commitMarkId;
            using (var writer = new StreamWriter(File.OpenWrite(importFile)) { NewLine = "\n" })
            {
                var index = new List<string>(transformationComposer.Transformations.Count);
                commitMarkId = WriteFastInsertImportFile(connection,
                                                         transformationComposer,
                                                         parents,
                                                         writer,
                                                         index,
                                                         description,
                                                         tempBranch,
                                                         beforeProcessing);
            }
            return ValidateAndUpdateHead(connection, description, importFile, parents, commitMarkId);
        }
        finally
        {
            connection.Repository.Branches.Remove(tempBranch);
            connection.Repository.Refs.Remove(tempBranch);
            TryDelete(importFile);
        }
    }

    private static int WriteFastInsertImportFile(IConnection connection,
                                                 TransformationComposer transformationComposer,
                                                 List<Commit> parents,
                                                 StreamWriter writer,
                                                 List<string> index,
                                                 CommitDescription description,
                                                 string tempBranch,
                                                 Action<ITransformation>? beforeProcessing)
    {
        var tip = connection.Repository.Info.IsHeadUnborn ? null : connection.Repository.Head.Tip;
        transformationComposer.ApplyTransformations(tip, writer, index, beforeProcessing);

        var commitMarkId = index.Count + 1;
        WriteFastInsertCommit(tempBranch, parents, writer, description, commitMarkId);
        WriteFastInsertCommitIndex(writer, index);

        return commitMarkId;
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

    private Commit ValidateAndUpdateHead(IConnection connection, CommitDescription description, string importFile, List<Commit> parents, int commitMarkId)
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
                                                      connection.Repository.Info.IsHeadUnborn,
                                                      parents.Count > 1);
        connection.Repository.UpdateHeadAndTerminalReference(commit, logMessage);

        return commit;
    }
}
