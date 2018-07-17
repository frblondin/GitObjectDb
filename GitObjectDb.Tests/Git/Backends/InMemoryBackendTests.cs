using AutoFixture.NUnit3;
using GitObjectDb.Git.Hooks;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GitObjectDb.Tests.Git.Backends
{
    public class InMemoryBackendTests
    {
        [Test]
        [AutoData]
        public void InMemoryBackend(InMemoryBackend sut, Signature signature, string message)
        {
            var path = GetTempPath();
            Repository.Init(path, true);
            using (var repository = new Repository(path))
            {
                repository.ObjectDatabase.AddBackend(sut, priority: 5);

                var definition = !repository.Info.IsHeadUnborn ? TreeDefinition.From(repository.Head.Tip.Tree) : new TreeDefinition();
                definition.Add("somefile.txt", repository.CreateBlob("foo"), Mode.NonExecutableFile);
                repository.Commit(
                    definition,
                    message,
                    signature, signature);
            }
        }

        static string GetTempPath() =>
            Path.Combine(Path.GetTempPath(), "Repos", Guid.NewGuid().ToString());
    }
}