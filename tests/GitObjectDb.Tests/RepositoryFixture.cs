using GitObjectDb.Git;
using GitObjectDb.IO;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace GitObjectDb.Tests
{
    [SetUpFixture]
    public class RepositoryFixture
    {
        private const string TempPath = @"C:\Temp";

        private static readonly string _workDirectory =
            Directory.Exists(TempPath) ?
            TempPath :
            Path.GetDirectoryName(AssemblyHelper.GetAssemblyPath(Assembly.GetExecutingAssembly()));

        public static string BenchmarkRepositoryPath { get; } =
            Path.Combine(_workDirectory, "Repos", "Benchmark");

        public static string TempRepoPath { get; } =
            Path.Combine(_workDirectory, "TempRepos");

        public static RepositoryDescription BenchmarkRepositoryDescription { get; } =
            new RepositoryDescription(BenchmarkRepositoryPath);

        public static string SmallRepositoryPath { get; } =
            Path.Combine(_workDirectory, "Repos", "Small");

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
