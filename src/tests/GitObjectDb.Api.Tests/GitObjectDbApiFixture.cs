using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace GitObjectDb.Api.Tests;

[SetUpFixture]
public class GitObjectDbApiFixture
{
    private const string TempPath = @"C:\Temp";

    private static readonly string _workDirectory =
        Directory.Exists(TempPath) ?
        TempPath :
        Path.GetDirectoryName(AssemblyHelper.GetAssemblyPath(Assembly.GetExecutingAssembly()));

    public static string SoftwareBenchmarkRepositoryPath { get; } =
        Path.Combine(_workDirectory, "Repos", "Data", "Software", "Benchmark");

    public static string TempRepoPath { get; } =
        Path.Combine(_workDirectory, "TempRepos");

    [OneTimeSetUp]
    public void RestoreRepositories()
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

    [OneTimeTearDown]
    public void DeleteTempPath() => DeleteTempPathImpl();

    private static void DeleteTempPathImpl()
    {
        DirectoryUtils.Delete(TempRepoPath, true);
    }
}
