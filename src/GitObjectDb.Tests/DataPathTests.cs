using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using Models.Software;
using NUnit.Framework;

namespace GitObjectDb.Tests
{
    public class DataPathTests : DisposeArguments
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
        public void GetParentNode(IConnection sut, Application application, Table table)
        {
            Assert.That(table.Path.GetParentNode(), Is.EqualTo(application.Path));
       }
    }
}