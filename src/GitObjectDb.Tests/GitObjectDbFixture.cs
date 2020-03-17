using GitObjectDb.Tests.Assets.Tools;
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
    public class GitObjectDbFixture
    {
        private const string TempPath = @"C:\Temp";

        private static readonly string _workDirectory =
            Directory.Exists(TempPath) ?
            TempPath :
            System.IO.Path.GetDirectoryName(AssemblyHelper.GetAssemblyPath(Assembly.GetExecutingAssembly()));

        public static string TempRepoPath { get; } =
            System.IO.Path.Combine(_workDirectory, "TempRepos");

        public static string GetAvailableFolderPath()
        {
            var i = 1;
            string result;
            while (true)
            {
                result = System.IO.Path.Combine(TempRepoPath, i.ToString("D4", CultureInfo.InvariantCulture));
                if (!Directory.Exists(result))
                {
                    return result;
                }
                i++;
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
