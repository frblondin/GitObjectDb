using GitObjectDb.Git;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GitObjectDb.Tests
{
    [SetUpFixture]
    public class RepositoryFixture
    {
        static string _lastTest;

        public static bool IsNewTest
        {
            get
            {
                var current = TestContext.CurrentContext.Test.ID;
                var result = !string.Equals(_lastTest, current, StringComparison.OrdinalIgnoreCase);
                _lastTest = current;
                return result;
            }
        }

        public static string GitPath
        {
            get
            {
                var result = (string)TestContext.CurrentContext.Test.Properties?.Get(nameof(GitPath));
                if (IsNewTest)
                {
                    DeleteTempPathImpl();
                    result = null;
                }

                if (result == null)
                {
                    result = GetRepositoryPath(TestContext.CurrentContext.Test.ID);
                    GitPath = result;
                }
                return result;
            }
            private set => TestContext.CurrentContext.Test.Properties.Set(nameof(GitPath), value);
        }

        public static string BenchmarkRepositoryPath =>
            Path.Combine(TestContext.CurrentContext.WorkDirectory, "Repos", "Benchmark");

        public static RepositoryDescription BenchmarkRepositoryDescription =>
            new RepositoryDescription(BenchmarkRepositoryPath);

        public static string GetRepositoryPath(string name) =>
            Path.Combine(TestContext.CurrentContext.WorkDirectory, "Repos", name);

        public static RepositoryDescription GetRepositoryDescription(OdbBackend backend = null) =>
            GetRepositoryDescription(GitPath, backend);

        public static RepositoryDescription GetRepositoryDescription(string path, OdbBackend backend = null) =>
            new RepositoryDescription(path, backend);

        [OneTimeSetUp]
        public void RestoreRepositories()
        {
            if (!Directory.Exists(Path.Combine(BenchmarkRepositoryPath, ".git")))
            {
                ZipFile.ExtractToDirectory("Assets\\Benchmark.zip", BenchmarkRepositoryPath);
            }
        }

        [OneTimeTearDown]
        public void DeleteTempPath() => DeleteTempPathImpl();

        static void DeleteTempPathImpl()
        {
            var path = GitPath;
            if (path != null)
            {
                DirectoryUtils.Delete(path);
            }
        }
    }
}
