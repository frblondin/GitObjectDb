using AutoFixture;
using AutoFixture.NUnit3;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Validations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Validations
{
    public class ModelValidationTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void FullValidationDoesNotFail(ObjectRepository sut)
        {
            // Act
            var result = sut.Validate(ValidationRules.All);

            // Assert
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LinkWithWrongRepositoryIsDetected(IObjectRepositoryContainer container, ObjectRepository repository)
        {
            // Arrange
            var linkField = repository.Flatten().OfType<Field>().FirstOrDefault(
                f => f.Content.MatchOrDefault(matchLink: l => true));

            // Act
            var failingLink = new LazyLink<Page>(container, new ObjectPath(UniqueId.CreateNew(), "foo"));
            var modified = repository.With(linkField, f => f.Content, FieldContent.NewLink(new FieldLinkContent(failingLink)));
            var result = modified.Repository.Validate();

            // Assert
            Assert.That(result, Has.Property(nameof(ValidationResult.IsValid)).False);
            Assert.That(result.Errors, Has.Exactly(1).Items);
            Assert.That(result.ToString(), Does.Contain("is not added to the dependencies"));
            Assert.That(result.Errors[0], Has.Property(nameof(ValidationFailure.PropertyName)).EqualTo("propertyName.Path"));
            Assert.That(result.Errors[0].Context.Instance, Is.EqualTo(modified));
            Assert.That(result.Errors[0].Context.Parent.Instance, Is.EqualTo(modified.Parent));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void LinkWithWrongObjectPathIsDetected(IObjectRepositoryContainer container, ObjectRepository repository)
        {
            // Arrange
            var linkField = repository.Flatten().OfType<Field>().FirstOrDefault(
                f => f.Content.MatchOrDefault(matchLink: l => true));

            // Act
            var failingLink = new LazyLink<Page>(container, new ObjectPath(linkField.Repository.Id, "foo"));
            var modified = repository.With(linkField, f => f.Content, FieldContent.NewLink(new FieldLinkContent(failingLink)));
            var result = modified.Repository.Validate();

            // Assert
            Assert.That(result, Has.Property(nameof(ValidationResult.IsValid)).False);
            Assert.That(result.Errors, Has.Exactly(1).Items);
            Assert.That(result.ToString(), Does.Contain("Unexisting object"));
            Assert.That(result.Errors[0], Has.Property(nameof(ValidationFailure.PropertyName)).EqualTo("propertyName.Path"));
            Assert.That(result.Errors[0].Context.Instance, Is.EqualTo(modified));
            Assert.That(result.Errors[0].Context.Parent.Instance, Is.EqualTo(modified.Parent));
        }
    }
}
