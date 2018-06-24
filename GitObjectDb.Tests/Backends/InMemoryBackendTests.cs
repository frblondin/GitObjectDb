using AutoFixture.NUnit3;
using GitObjectDb.Backends;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GitObjectDb.Tests.Backends
{
    public class InMemoryBackendTests
    {
        [Test, AutoData]
        public void InMemoryBackend(InMemoryBackend sut, Signature signature, string message)
        {
            var path = GetTempPath();
            Repository.Init(path, true);
            using (var repository = new Repository(path))
            {
                repository.ObjectDatabase.AddBackend(sut, priority: 5);

                repository.Commit(
                    (r, d) => d.Add("somefile.txt", r.CreateBlob(new StringBuilder("foo")), Mode.NonExecutableFile),
                    message, signature, signature);
            }
        }

        static string GetTempPath() =>
            Path.Combine(Path.GetTempPath(), "Repos", Guid.NewGuid().ToString());

    }
}
