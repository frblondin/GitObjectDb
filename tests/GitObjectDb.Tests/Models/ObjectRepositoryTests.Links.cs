using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public partial class ObjectRepositoryTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LoadLinkField(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange
            container.AddRepository(sut, signature, message);
            var linkField = sut.Flatten().OfType<Field>().First(
                f => f.Content.MatchOrDefault(matchLink: l => true));

            // Act
            var newContainer = containerFactory.Create<ObjectRepository>(container.Path);
            var loaded = newContainer.Repositories.Single();
            var loadedLinkField = (Field)loaded.GetFromGitPath(linkField.GetFolderPath());

            // Assert
            var target = linkField.Content.MatchOrDefault(matchLink: c => c.Target).Link;
            var newTarget = loadedLinkField.Content.MatchOrDefault(matchLink: c => c.Target).Link;
            Assert.That(newTarget.Id, Is.EqualTo(target.Id));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ResolveLinkReferers(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Arrange
            sut = container.AddRepository(sut, signature, message);
            var linkField = sut.Flatten().OfType<Field>().First(
                f => f.Content.MatchOrDefault(matchLink: l => true));

            // Act
            var target = linkField.Content.MatchOrDefault(matchLink: c => c.Target).Link;
            var referrers = sut.GetReferrers(target);

            // Assert
            Assert.That(referrers, Does.Contain(linkField));
        }
    }
}
