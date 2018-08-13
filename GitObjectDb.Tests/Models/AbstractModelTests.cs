using GitObjectDb;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using PowerAssert;
using System.Collections.Generic;
using System.Reflection;

namespace GitObjectDb.Tests.Models
{
    public class AbstractModelTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void WithModifiesValue(Page page, string newName)
        {
            // Act
            var modified = page.With(p => p.Name == newName);

            // Assert
            Assert.That(modified.Name, Is.EqualTo(newName));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void WithModifiesLink(LinkField field, Page newLinkedPage)
        {
            // Act
            var modified = field.With(f => f.PageLink == new LazyLink<Page>(newLinkedPage));

            // Assert
            Assert.That(modified.PageLink.Link, Is.EqualTo(newLinkedPage));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void WithDuplicatesImmutableMetadataTree(Page page, string newName)
        {
            // Act
            var modified = page.With(p => p.Name == newName);

            // Assert
            PAssert.IsTrue(AreFunctionnally.Equivalent<Page>(() => page == modified, nameof(Page.Name)));
        }
    }
}
