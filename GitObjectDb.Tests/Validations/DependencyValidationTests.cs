using FluentValidation.Results;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Validations
{
    public class DependencyValidationTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ThrowIfMissingDependency(ObjectRepositoryContainer<ObjectRepository> container, ObjectRepository repository, Signature signature, string message)
        {
            // Arrange
            var wrongDependency = new RepositoryDependency(Guid.NewGuid(), "foo", new System.Version(1, 0));
            repository = repository.With(r => r.Dependencies == repository.Dependencies.Add(wrongDependency));
            container.AddRepository(repository, signature, message);

            // Act
            var result = container.Validate();

            // Assert
            Assert.That(result.ToString(), Does.Contain("could not be found"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void ThrowIfWrongDependencyVersion(ObjectRepositoryContainer<ObjectRepository> container, ObjectRepository repository, ObjectRepository dependency, Signature signature, string message)
        {
            // Arrange
            container.AddRepository(dependency, signature, message);
            var wrongDependency = new RepositoryDependency(dependency, new System.Version(dependency.Version.Major + 1, 0));
            repository = repository.With(r => r.Dependencies == repository.Dependencies.Add(wrongDependency));
            container.AddRepository(repository, signature, message);

            // Act
            var result = container.Validate();

            // Assert
            Assert.That(result.ToString(), Does.Contain("should be of version"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void NoValidationErrorIfVersionIsLowerOrEqual(ObjectRepositoryContainer<ObjectRepository> container, ObjectRepository repository, ObjectRepository dependency, Signature signature, string message)
        {
            // Arrange
            container.AddRepository(dependency, signature, message);
            var wrongDependency = new RepositoryDependency(dependency, new System.Version(dependency.Version.Major - 1, 0));
            repository = repository.With(r => r.Dependencies == repository.Dependencies.Add(wrongDependency));
            container.AddRepository(repository, signature, message);

            // Act
            var result = container.Validate();

            // Assert
            Assert.That(result, Has.Property(nameof(ValidationResult.IsValid)).True);
        }
    }
}
