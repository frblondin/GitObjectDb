using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Transformations;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Tests.Transformations
{
    public class PropertyTransformationTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PropertyTransformationWorksForModifiableProperties(Page page)
        {
            // Act
            var sut = new PropertyTransformation(page, CreateExpression<Page>(p => p.Name), null);

            // Assert
            Assert.That(sut.PropertyName, Is.EqualTo(nameof(Page.Name)));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PropertyTransformationFailsForNonModifiableProperties(Page page)
        {
            Assert.Throws<GitObjectDbException>(() =>
                new PropertyTransformation(page, CreateExpression<Page>(p => p.Id), null));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PropertyTransformationFailsForIndexedProperties(Page page)
        {
            Assert.Throws<GitObjectDbException>(() =>
                new PropertyTransformation(page, CreateExpression<Page>(p => p.Fields[0].Name), null));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PropertyTransformationFailsForMethodCall(Page page)
        {
            Assert.Throws<GitObjectDbException>(() =>
                new PropertyTransformation(page, CreateExpression<Page>(p => p.ToString()), null));
        }

        private static Expression<Func<T, object>> CreateExpression<T>(Expression<Func<T, object>> expression) => expression;
    }
}
