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
    private readonly ITreeValidation _treeValidation;

    public FastImportCommitCommand(ITreeValidation treeValidation)
    {
        _treeValidation = treeValidation;

        GitCliCommand.ThrowIfGitNotInstalled();
    }

    public Commit Commit(IConnection connection,
                         TransformationComposer transformationComposer,
                         CommitDescription description,
                         Action<ITransformation>? beforeProcessing = null)
    {
        var importFile = Path.GetTempFileName();
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
                                                         beforeProcessing);
            }
            var commit = SendCommandThroughCli(connection, importFile, commitMarkId);
            _treeValidation.Validate(commit.Tree, connection.Model);
            return commit;
        }
        finally
        {
            TryDelete(importFile);
        }
    }

    private static int WriteFastInsertImportFile(IConnection connection,
                                                 TransformationComposer transformationComposer,
                                                 List<Commit> parents,
                                                 StreamWriter writer,
                                                 List<string> index,
                                                 CommitDescription description,
                                                 Action<ITransformation>? beforeProcessing)
    {
        var tip = connection.Repository.Info.IsHeadUnborn ? null : connection.Repository.Head.Tip;
        transformationComposer.ApplyTransformations(tip, writer, index, beforeProcessing);

        var commitMarkId = index.Count + 1;
        WriteFastInsertCommit(connection, parents, writer, description, commitMarkId);
        WriteFastInsertCommitIndex(writer, index);

        return commitMarkId;
    }

    private static void WriteFastInsertCommit(IConnection connection,
                                              List<Commit> parents,
                                              TextWriter writer,
                                              CommitDescription description,
                                              int commitMarkId)
    {
        var branch = connection.Repository.Refs.Head.TargetIdentifier;
        if (parents.Count == 0)
        {
            writer.WriteLine($"reset {branch}");
        }
        writer.WriteLine($"commit {branch}");
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
        var markFile = Path.GetTempFileName();
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
        }
    }
}
