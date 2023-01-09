using GitObjectDb.Api.ProtoBuf.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System.IO;

namespace GitObjectDb.Api.ProtoBuf.Tests;
[SetUpFixture]
public class ProtoBufFixture
{
    [OneTimeSetUp]
    public void CleanUpPastExecutions()
    {
        DirectoryUtils.Delete(ConnectionProvider.ReposPath, continueOnError: true);
    }
}
