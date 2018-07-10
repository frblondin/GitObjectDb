using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
                    result = Path.Combine(Path.GetTempPath(), "Repos", Guid.NewGuid().ToString());
                    GitPath = result;
                }
                return result;
            }
            private set => TestContext.CurrentContext.Test.Properties.Set(nameof(GitPath), value);
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
