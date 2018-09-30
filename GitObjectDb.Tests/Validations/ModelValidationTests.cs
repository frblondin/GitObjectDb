using AutoFixture;
using AutoFixture.NUnit3;
using FluentValidation;
using FluentValidation.Results;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Validations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Validations
{
    public class ModelValidationTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void FullValidationDoesNotFail(ObjectRepository sut)
        {
            sut.Validate(ValidationRules.All);
        }

        [Test]
        [AutoData]
        public void ValidationFailsForNestedObject(IFixture fixture)
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddGitObjectDb();
            services.AddSingleton<IValidatorFactory, CustomValidatorFactory<Field>>();
            var serviceProvider = services.BuildServiceProvider();
            fixture.Inject<IServiceProvider>(serviceProvider);
            fixture.Customize(new MetadataCustomization());
            var repository = fixture.Create<ObjectRepository>();

            // Act
            var result = repository.Validate();

            // Assert
            Assert.That(result, Has.Property(nameof(ValidationResult.IsValid)).False);
            Assert.That(result.ToString(), Does.Contain("'Name' should be equal to 'foo'."));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void LinkWithWrongRepositoryIsDetected(LinkField linkField)
        {
            // Act
            var modified = linkField.With(f => f.PageLink == new LazyLink<Page>(new ObjectPath(UniqueId.CreateNew(), "foo")));
            var result = modified.Repository.Validate();

            // Assert
            Assert.That(result, Has.Property(nameof(ValidationResult.IsValid)).False);
            Assert.That(result.ToString(), Does.Contain("which is not added to the dependencies"));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void LinkWithWrongObjectPathIsDetected(LinkField linkField)
        {
            // Act
            var modified = linkField.With(f => f.PageLink == new LazyLink<Page>(new ObjectPath(linkField.Repository.Id, "foo")));
            var result = modified.Repository.Validate();

            // Assert
            Assert.That(result, Has.Property(nameof(ValidationResult.IsValid)).False);
            Assert.That(result.ToString(), Does.Contain("Path is referring an unexisting object"));
        }

        public class CustomValidatorFactory<TTarget> : ValidatorFactory
            where TTarget : AbstractModel
        {
            readonly IServiceProvider _serviceProvider;

            public CustomValidatorFactory(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }

            protected override IValidator Resolve(Type targetType)
            {
                if (targetType == typeof(TTarget))
                {
                    return new TargetValidation<TTarget>(_serviceProvider);
                }
                return base.Resolve(targetType);
            }
        }

        class TargetValidation<TTarget> : AbstractValidator<TTarget>
            where TTarget : AbstractModel
        {
            public TargetValidation(IServiceProvider serviceProvider)
            {
                Include(new MetadataObjectValidator<TTarget>(serviceProvider));
                RuleFor(p => p.Name).Equal("foo");
            }

            public override ValidationResult Validate(ValidationContext<TTarget> context)
            {
                context.GetType();
                return base.Validate(context);
            }
        }
    }
}
