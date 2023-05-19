using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace GitObjectDb.Tests;

[SetUpFixture]
public class GitObjectDbFixture
{
    private static readonly object _sync = new();
    private static readonly string _workDirectory =
        Path.GetDirectoryName(AssemblyHelper.GetAssemblyPath(Assembly.GetExecutingAssembly()));

    public static string SoftwareBenchmarkRepositoryPath { get; } =
        Path.Combine(_workDirectory, "Repos", "Data", "Software", "Benchmark");

    public static string TempRepoPath { get; } =
        Path.Combine(_workDirectory, "TempRepos");

#pragma warning disable NUnit1028 // The non-test method is public
    public static string GetAvailableFolderPath()
#pragma warning restore NUnit1028 // The non-test method is public
    {
        var test = TestContext.CurrentContext.Test;
        var result = Path.Combine(TempRepoPath, $"{test.MethodName}-{test.ID}");
        DirectoryUtils.Delete(result, true);
        Directory.CreateDirectory(result);
        return result;
    }

    [OneTimeSetUp]
    public void RestoreRepositories()
    {
        lock (_sync)
        {
            if (!Directory.Exists(SoftwareBenchmarkRepositoryPath))
            {
                ZipFile.ExtractToDirectory(
                    Path.Combine(
                        TestContext.CurrentContext.TestDirectory,
                        "Assets", "Data", "Software", "Benchmark.zip"),
                    SoftwareBenchmarkRepositoryPath);
            }
        }
    }
}
