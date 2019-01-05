using GitObjectDb;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using PowerAssert;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GitObjectDb.Tests.Models
{
    public class AbstractModelTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void WithModifiesLink(ObjectRepository repository, Page newLinkedPage)
        {
            // Arrange
            var field = repository.Flatten().OfType<Field>().First(
                f => f.Content.Match(() => false, matchLink: l => true));

            // Act
            var modified = repository.With(field, f => f.Content, FieldContent.NewLink(new FieldLinkContent(new LazyLink<Page>(repository.Container, newLinkedPage))));
            var link = modified.Content.MatchOrDefault(matchLink: l => l.Target);

            // Assert
            Assert.That(link.Link, Is.EqualTo(newLinkedPage));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void WithDuplicatesImmutableObjectRepository(ObjectRepository repository, Page page, string newName)
        {
            // Act
            var modified = repository.With(page, p => p.Name, newName);

            // Assert
            PAssert.IsTrue(AreFunctionnally.Equivalent<Page>(() => page == modified, nameof(Page.Name)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void CloneableParametersGetCloned(ObjectRepository repository, string newName)
        {
            // Arrange
            var linkField = repository.Flatten().OfType<Field>().First(f => f.Content.IsLink);

            // Act
            var modified = repository.With(linkField, l => l.Name, newName);

            // Assert
            var oldLink = linkField.Content.MatchOrDefault(matchLink: l => l.Target);
            var newLink = modified.Content.MatchOrDefault(matchLink: l => l.Target);
            Assert.That(newLink.Link, Is.Not.SameAs(oldLink));
        }
    }
}
