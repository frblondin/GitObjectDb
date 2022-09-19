using GitObjectDb.Tools;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Internal.Commands
{
    internal class FastImportCommitCommand : ICommitCommand
    {
        private readonly ITreeValidation _treeValidation;

        public FastImportCommitCommand(ITreeValidation treeValidation)
        {
            _treeValidation = treeValidation;

            GitCliCommand.ThrowIfGitNotInstalled();
        }

        public Commit Commit(IConnectionInternal connection, TransformationComposer transformationComposer, string message, Signature author, Signature committer, bool amendPreviousCommit = false, Commit? mergeParent = null, Action<ITransformation>? beforeProcessing = null)
        {
            var importFile = Path.GetTempFileName();
            try
            {
                var parents = CommitCommand.RetrieveParentsOfTheCommitBeingCreated(connection.Repository, amendPreviousCommit, mergeParent).ToList();
                int commitMarkId;
                using (var writer = new StreamWriter(File.OpenWrite(importFile)))
                {
                    var index = new List<string>(transformationComposer.Transformations.Count);
                    commitMarkId = WriteFastInsertImportFile(connection, transformationComposer, parents, writer, index, message, author, committer, beforeProcessing);
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

        private static int WriteFastInsertImportFile(IConnectionInternal connection, TransformationComposer transformationComposer, List<Commit> parents, StreamWriter writer, List<string> index, string message, Signature author, Signature committer, Action<ITransformation>? beforeProcessing)
        {
            var tip = connection.Info.IsHeadUnborn ? null : connection.Head.Tip;
            transformationComposer.ApplyTransformations(tip, writer, index, beforeProcessing);

            var commitMarkId = index.Count + 1;
            WriteFastInsertCommit(connection, parents, writer, message, author, committer, commitMarkId);
            WriteFastInsertCommitIndex(writer, index);

            return commitMarkId;
        }

        private static void WriteFastInsertCommit(IConnectionInternal connection, List<Commit> parents, StreamWriter writer, string message, Signature author, Signature committer, int commitMarkId)
        {
            var branch = connection.Repository.Refs.Head.TargetIdentifier;
            if (parents.Count == 0)
            {
                writer.Write($"reset {branch}\n");
            }
            writer.Write($"commit {branch}\n");
            writer.Write($"mark :{commitMarkId}\n");
            WriteSignature(writer, nameof(author), author);
            WriteSignature(writer, nameof(committer), committer);
            writer.Write($"data {Encoding.UTF8.GetByteCount(message)}\n");
            writer.Write(message);
            writer.Write('\n');
            if (parents.Count >= 1)
            {
                writer.Write($"from {parents[0].Id}\n");
                if (parents.Count >= 2)
                {
                    writer.Write($"merge {parents[1].Id}\n");
                }
            }

            static void WriteSignature(StreamWriter writer, string type, Signature signature)
            {
                writer.Write($"{type} {signature.Name} <{signature.Email}> {signature.When.ToUnixTimeSeconds()} {signature.When.Offset.Minutes:+0000;-0000}\n");
            }
        }

        private static void WriteFastInsertCommitIndex(StreamWriter writer, List<string> index)
        {
            foreach (var item in index)
            {
                writer.Write(item);
                writer.Write('\n');
            }
        }

        private static Commit SendCommandThroughCli(IConnectionInternal connection, string importFile, int commitMarkId)
        {
            var markFile = Path.GetTempFileName();
            try
            {
                using var stream = File.OpenRead(importFile);
                GitCliCommand.Execute(connection.Info.WorkingDirectory, @$"fast-import --export-marks=""{markFile}""", stream);
                var linePrefix = $":{commitMarkId} ";
                var line = File.ReadLines(markFile).FirstOrDefault(l => l.StartsWith(linePrefix)) ??
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
}
