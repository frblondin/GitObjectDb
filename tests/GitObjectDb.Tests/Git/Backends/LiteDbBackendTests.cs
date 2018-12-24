using AutoFixture.NUnit3;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Models;
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
    public class LiteDbBackendTests
    {
        [Test]
        [AutoData]
        public void LiteDbBackend(Signature signature, string message)
        {
            var path = GetTempPath();
            Repository.Init(path, true);
            using (var repository = new Repository(path))
            {
                var sut = new LiteDbBackend(Path.Combine(path, "lite.db"));
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
            Path.Combine(Path.GetTempPath(), "Repos", UniqueId.CreateNew().ToString());
    }
}