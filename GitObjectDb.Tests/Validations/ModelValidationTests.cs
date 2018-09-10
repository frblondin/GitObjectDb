using AutoFixture;
using AutoFixture.NUnit3;
using FluentValidation;
using FluentValidation.Results;
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
            services.AddSingleton<IValidatorFactory, CustomValidatorFactory>();
            var serviceProvider = services.BuildServiceProvider();
            fixture.Inject<IServiceProvider>(serviceProvider);
            fixture.Customize(new MetadataCustomization());
            var repository = fixture.Create<ObjectRepository>();

            // Act
            var result = repository.Validate();

            // Assert
            Assert.That(result, Has.Property(nameof(ValidationResult.IsValid)).False);
            Assert.That(result.ToString(), Does.Contain("'Name' should not be equal to 'Field 2'."));
        }

        public class CustomValidatorFactory : ValidatorFactory
        {
            readonly IServiceProvider _serviceProvider;

            public CustomValidatorFactory(IServiceProvider serviceProvider)
                : base(serviceProvider)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }

            protected override IValidator Resolve(Type targetType)
            {
                if (targetType == typeof(Field))
                {
                    return new FieldValidation(_serviceProvider);
                }
                return base.Resolve(targetType);
            }
        }

        class FieldValidation : AbstractValidator<Field>
        {
            public FieldValidation(IServiceProvider serviceProvider)
            {
                Include(new MetadataObjectValidator<Field>(serviceProvider));
                RuleFor(p => p.Name).NotEqual("Field 2");
            }

            public override ValidationResult Validate(ValidationContext<Field> context)
            {
                context.GetType();
                return base.Validate(context);
            }
        }
    }
}
