using System;
using System.Diagnostics;
using System.IO;

namespace GitObjectDb.Internal.Commands
{
    internal static class GitCliCommand
    {
        private static readonly Lazy<bool> _isGitInstalled = new(
            () => Execute(Environment.CurrentDirectory, "--version", throwOnError: false) == 0);

        internal static bool IsGitInstalled => _isGitInstalled.Value;

        internal static int Execute(string repository, string arguments, Stream? stream = null, bool throwOnError = true)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = repository,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
            });

            if (stream is not null)
            {
                CopyStreamToInput(stream, process);
            }

            process.WaitForExit();

            if (throwOnError)
            {
                ThrowIfError(process);
            }
            return process.ExitCode;
        }

        private static void CopyStreamToInput(Stream stream, Process process)
        {
            try
            {
                stream.CopyTo(process.StandardInput.BaseStream);
            }
            catch
            {
            }
            process.StandardInput.Close();
        }

        private static void ThrowIfError(Process process)
        {
            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new GitObjectDbException($"Git command failed: " + error);
            }
        }
    }
}
