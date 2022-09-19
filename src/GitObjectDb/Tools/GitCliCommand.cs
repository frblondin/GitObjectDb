using System;
using System.Diagnostics;
using System.IO;

namespace GitObjectDb.Tools
{
    /// <summary>Provides ability to run Git CLI commands.</summary>
    public static class GitCliCommand
    {
        private static readonly Lazy<bool> _isGitInstalled = new(
            () => ExecuteNoCheck(Environment.CurrentDirectory, "--version", throwOnError: false) == 0);

        /// <summary>Gets a value indicating whether Git CLI is accessible.</summary>
        public static bool IsGitInstalled => _isGitInstalled.Value;

        internal static void ThrowIfGitNotInstalled()
        {
            if (!IsGitInstalled)
            {
                throw new GitObjectDbException("Git doesn't seem to be installed or is not accessible.");
            }
        }

        /// <summary>Executes a GIT command.</summary>
        /// <param name="repository">The path of the repository.</param>
        /// <param name="arguments">The command to execute.</param>
        /// <param name="inputStream">The input stream (optional).</param>
        /// <param name="throwOnError">Whether an exception should be thrown if command failed.</param>
        /// <returns>The command exit status.</returns>
        public static int Execute(string repository, string arguments, Stream? inputStream = null, bool throwOnError = true)
        {
            ThrowIfGitNotInstalled();

            return ExecuteNoCheck(repository, arguments, inputStream, throwOnError);
        }

        private static int ExecuteNoCheck(string repository, string arguments, Stream? inputStream = null, bool throwOnError = true)
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

            if (inputStream is not null)
            {
                CopyStreamToInput(process, inputStream);
            }

            process.WaitForExit();

            if (throwOnError)
            {
                ThrowIfError(process);
            }

            return process.ExitCode;
        }

        private static void CopyStreamToInput(Process process, Stream stream)
        {
            stream.CopyTo(process.StandardInput.BaseStream);
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
