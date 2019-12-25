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
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Services
{
    public class ObjectRepositorySearchTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task SearchAsync(IObjectRepositorySearch search, ObjectRepository repository, Field field, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Arrange
            await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);

            // Act
            var found = await search.GrepAsync(container, field.Id.ToString()).ToListAsync();

            // Assert
            Assert.That(found, Is.Not.Empty);
        }
    }
}
