using GitObjectDb.Tests;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace GitObjectDb.Api.OData.Tests;

[SetUpFixture]
public class GitObjectDbApiFixture
{
    [OneTimeSetUp]
    public void RestoreRepositories() => new GitObjectDbFixture().RestoreRepositories();
}
