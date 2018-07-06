using AutoFixture.NUnit3;
using LibGit2Sharp;
using NUnit.Framework;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GitObjectDb.Tests.Git.Backends
{
    public class RedisBackendTests
    {
        const string Host = "localhost";

        [Test]
        [AutoData]
        public void RedisBackend(Signature signature, string message)
        {
            var path = GetTempPath();
            Repository.Init(path, true);
            using (var repository = new Repository(path))
            {
                var sut = new RedisBackend(Host);
                repository.ObjectDatabase.AddBackend(sut, priority: 5);

                repository.Commit(
                    (r, d) => d.Add("somefile.txt", r.CreateBlob(new StringBuilder("foo")), Mode.NonExecutableFile),
                    message, signature, signature);
            }
        }

        [SetUp]
        public void Init()
        {
            if (!IsRedisRunning())
            {
                Assert.Inconclusive("Redis is not accessible, Redis tests are disabled.");
            }
        }

        static string GetTempPath() =>
            Path.Combine(Path.GetTempPath(), "Repos", Guid.NewGuid().ToString());

        static bool IsRedisRunning()
        {
            try
            {
                using (ConnectionMultiplexer.Connect(Host))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}