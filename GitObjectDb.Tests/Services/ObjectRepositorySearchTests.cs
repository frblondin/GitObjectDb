using GitObjectDb.Git;
using GitObjectDb.Models;
using GitObjectDb.Services;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Services
{
    public class ObjectRepositorySearchTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void Search(IObjectRepositorySearch search, ObjectRepository repository, Field field, ObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Arrange
            container.AddRepository(repository, signature, message);

            // Act
            var found = search.Grep(container, field.Id.ToString()).ToList();

            // Assert
            Assert.That(found, Has.Count.EqualTo(1));
        }
    }
}
