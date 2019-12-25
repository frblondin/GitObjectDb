using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Validations
{
    public class DependencyValidationTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task ThrowIfMissingDependencyAsync(IObjectRepositoryContainer<ObjectRepository> container, ObjectRepository repository, Signature signature, string message)
        {
            // Arrange
            var wrongDependency = new RepositoryDependency(UniqueId.CreateNew(), "foo", new System.Version(1, 0));
            repository = repository.WithAsync(repository, r => r.Dependencies, repository.Dependencies.Add(wrongDependency));
            await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);

            // Act
            var result = container.Validate();

            // Assert
            Assert.That(result.ToString(), Does.Contain("could not be found"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task ThrowIfWrongDependencyVersionAsync(IObjectRepositoryContainer<ObjectRepository> container, ObjectRepository repository, ObjectRepository dependency, Signature signature, string message)
        {
            // Arrange
            await container.AddRepositoryAsync(dependency, signature, message).ConfigureAwait(false);
            var wrongDependency = new RepositoryDependency(dependency, new System.Version(dependency.Version.Major + 1, 0));
            repository = repository.WithAsync(repository, r => r.Dependencies, repository.Dependencies.Add(wrongDependency));
            await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);

            // Act
            var result = container.Validate();

            // Assert
            Assert.That(result.ToString(), Does.Contain("should be of version"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task NoValidationErrorIfVersionIsLowerOrEqualAsync(IObjectRepositoryContainer<ObjectRepository> container, ObjectRepository repository, ObjectRepository dependency, Signature signature, string message)
        {
            // Arrange
            await container.AddRepositoryAsync(dependency, signature, message).ConfigureAwait(false);
            var newDependency = new RepositoryDependency(dependency, new System.Version(dependency.Version.Major - 1, 0));
            repository = repository.WithAsync(repository, r => r.Dependencies, repository.Dependencies.Add(newDependency));
            await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);

            // Act
            var result = container.Validate();

            // Assert
            Assert.That(result.IsValid, Is.True);
        }
    }
}
