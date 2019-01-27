using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public class LazyLinkTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LazyLinkDoesNotCacheLambdaResult(Field parent, Page page)
        {
            // Arrange
            var count = 0;
            var sut = new LazyLink<Page>(parent.Container, () =>
            {
                count++;
                return page;
            });

            // Act
            var resolved = sut.Link;
            var yetAgainResolved = sut.Link;

            // Assert
            Assert.That(resolved, Is.SameAs(page));
            Assert.That(yetAgainResolved, Is.SameAs(page));
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LazyLinkCloneDuplicatesFactoryWithoutInvokingIt(IObjectRepositoryContainer<ObjectRepository> container)
        {
            // Act
            var exception = new Exception();
            var sut = (LazyLink<Page>)new LazyLink<Page>(container, () => throw exception).Clone();

            // Assert
            Assert.That(sut.IsLinkCreated, Is.False);
            var thrown = Assert.Throws<Exception>(() => sut.Link.ToString());
            Assert.That(thrown, Is.SameAs(exception));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LazyLinkCloneCopiesPath(IObjectRepositoryContainer<ObjectRepository> container, Page page)
        {
            // Act
            var sut = (LazyLink<Page>)new LazyLink<Page>(container, page.Path).Clone();

            // Assert
            Assert.That(sut.IsLinkCreated, Is.False);
            Assert.That(sut.Path, Is.EqualTo(page.Path));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LazyLinkCloneCopiesObjectPath(IObjectRepositoryContainer<ObjectRepository> container, Page page)
        {
            // Act
            var sut = (LazyLink<Page>)new LazyLink<Page>(container, page).Clone();

            // Assert
            Assert.That(sut.IsLinkCreated, Is.False);
            Assert.That(sut.Path, Is.EqualTo(page.Path));
        }
    }
}
