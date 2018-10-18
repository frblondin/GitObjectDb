using AutoFixture.NUnit3;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Validations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Validations
{
    public class ValidatorFactoryTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization))]
        public void GenericValidatorIsRetrieved(ValidatorFactory sut)
        {
            // Assert
            Assert.IsInstanceOf<ObjectRepositoryContainerValidator<ObjectRepository>>(sut.GetValidator<ObjectRepositoryContainer<ObjectRepository>>());
        }
    }
}
