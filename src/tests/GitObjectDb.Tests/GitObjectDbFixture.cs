using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Tests;

[SetUpFixture]
public class GitObjectDbFixture
{
    private const string TempPath = @"C:\Temp";

    private static readonly object _sync = new();
    private static readonly string _workDirectory =
        Directory.Exists(TempPath) ?
        TempPath :
        Path.GetDirectoryName(AssemblyHelper.GetAssemblyPath(Assembly.GetExecutingAssembly()));

    public static string SoftwareBenchmarkRepositoryPath { get; } =
        Path.Combine(_workDirectory, "Repos", "Data", "Software", "Benchmark");

    public static string TempRepoPath { get; } =
        Path.Combine(_workDirectory, "TempRepos");

#pragma warning disable NUnit1028 // The non-test method is public
    public static string GetAvailableFolderPath()
#pragma warning restore NUnit1028 // The non-test method is public
    {
        var i = 1;
        string result;
        lock (_sync)
        {
            while (true)
            {
                result = Path.Combine(TempRepoPath, i.ToString("D4", CultureInfo.InvariantCulture));
                if (!Directory.Exists(result))
                {
                    Directory.CreateDirectory(result);
                    return result;
                }
                i++;
            }
        }
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

    [OneTimeSetUp]
    [OneTimeTearDown]
    public void DeleteTempPath() => DirectoryUtils.Delete(TempRepoPath, true);
}
