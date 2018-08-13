using GitObjectDb.Compare;
using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Tools;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Git.Backends;
using LibGit2Sharp;
using NUnit.Framework;
using PowerAssert;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public partial class ObjectRepositoryTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void LoadLinkField(IObjectRepositoryLoader loader, ObjectRepository sut, LinkField linkField, Signature signature, string message, InMemoryBackend inMemoryBackend)
        {
            // Act
            sut.SaveInNewRepository(signature, message, RepositoryFixture.GetRepositoryDescription(inMemoryBackend));
            var loaded = loader.LoadFrom<ObjectRepository>(RepositoryFixture.GetRepositoryDescription(inMemoryBackend));
            var loadedLinkField = (LinkField)loaded.GetFromGitPath(linkField.GetFolderPath());

            // Assert
            Assert.That(loadedLinkField.PageLink.Link.Id, Is.EqualTo(linkField.PageLink.Link.Id));
        }
    }
}
