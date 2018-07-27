using AutoFixture.NUnit3;
using GitObjectDb.Reflection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace GitObjectDb.Tests.Reflection
{
    public class PredicateReflectorValueVisitorTests
    {
        [Test]
        [AutoData]
        public void PredicateReflectorExtractsConstantValue(string value)
        {
            // Act
            var extracted = PredicateReflector.ValueVisitor.ExtractValue(CreateExpression(() => value));

            // Assert
            Assert.That(extracted, Is.EqualTo(value));
        }

        [Test]
        [AutoData]
        public void PredicateReflectorExtractsDefaultValue()
        {
            // Act
            var extracted = PredicateReflector.ValueVisitor.ExtractValue(CreateExpression(() => default(string)));

            // Assert
            Assert.That(extracted, Is.Null);
        }

        [Test]
        [AutoData]
        public void PredicateReflectorExtractsPropertyValue(string value)
        {
            // Act
            var extracted = PredicateReflector.ValueVisitor.ExtractValue(CreateExpression(() => value.Length));

            // Assert
            Assert.That(extracted, Is.EqualTo(value.Length));
        }

        [Test]
        [AutoData]
        public void PredicateReflectorExtractsConstantValue(IList<string> values)
        {
            // Act
            var extracted = PredicateReflector.ValueVisitor.ExtractValue(CreateExpression(() => values[1]));

            // Assert
            Assert.That(extracted, Is.EqualTo(values[1]));
        }

        [Test]
        [AutoData]
        public void PredicateReflectorExtractsMethodCallValue(string value)
        {
            // Act
            var extracted = PredicateReflector.ValueVisitor.ExtractValue(CreateExpression(() => value.Substring(5)));

            // Assert
            Assert.That(extracted, Is.EqualTo(value.Substring(5)));
        }

        Expression CreateExpression<T>(Expression<Func<T>> expression) => expression.Body;
    }
}
