using GitObjectDb.Git;
using GitObjectDb.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GitObjectDb.Tests
{
    [SetUpFixture]
    public class RepositoryFixture
    {
        private const string TempPath = @"C:\Temp";

        private static string WorkDirectory => Directory.Exists(TempPath) ? TempPath : TestContext.CurrentContext.WorkDirectory;

        public static string BenchmarkRepositoryPath =>
            Path.Combine(WorkDirectory, "Repos", "Benchmark");

        public static string TempRepoPath =>
            Path.Combine(WorkDirectory, "TempRepos");

        public static RepositoryDescription BenchmarkRepositoryDescription =>
            new RepositoryDescription(BenchmarkRepositoryPath);

        public static string SmallRepositoryPath =>
            Path.Combine(WorkDirectory, "Repos", "Small");

        public static string GetRepositoryPath(string name) =>
            Path.Combine(TempRepoPath, name);

        public static string GetAvailableFolderPath()
        {
            var i = 1;
            string result;
            while (true)
            {
                result = Path.Combine(TempRepoPath, i.ToString("D4", CultureInfo.InvariantCulture));
                if (!Directory.Exists(result))
                {
                    return result;
                }
                i++;
            }
        }

        [OneTimeSetUp]
        public void RestoreRepositories()
        {
            if (!Directory.Exists(Path.Combine(BenchmarkRepositoryPath, ".git")))
            {
                ZipFile.ExtractToDirectory("Assets\\Benchmark.zip", BenchmarkRepositoryPath);
            }
            if (!Directory.Exists(Path.Combine(SmallRepositoryPath, ".git")))
            {
                ZipFile.ExtractToDirectory("Assets\\Small.zip", SmallRepositoryPath);
            }
        }

        [OneTimeTearDown]
        public void DeleteTempPath() => DeleteTempPathImpl();

        private static void DeleteTempPathImpl()
        {
            DirectoryUtils.Delete(TempRepoPath, true);
        }
    }
}
