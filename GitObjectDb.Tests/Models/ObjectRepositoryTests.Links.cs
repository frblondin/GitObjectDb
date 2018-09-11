using GitObjectDb.Compare;
using GitObjectDb.Git;
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
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void LoadLinkField(ObjectRepository sut, IObjectRepositoryContainer<ObjectRepository> container, IServiceProvider serviceProvider, LinkField linkField, Signature signature, string message)
        {
            // Arrange
            container.AddRepository(sut, signature, message);

            // Act
            var newContainer = new ObjectRepositoryContainer<ObjectRepository>(serviceProvider, container.Path);
            var loaded = newContainer.Repositories.Single();
            var loadedLinkField = (LinkField)loaded.GetFromGitPath(linkField.GetFolderPath());

            // Assert
            Assert.That(loadedLinkField.PageLink.Link.Id, Is.EqualTo(linkField.PageLink.Link.Id));
        }
    }
}
