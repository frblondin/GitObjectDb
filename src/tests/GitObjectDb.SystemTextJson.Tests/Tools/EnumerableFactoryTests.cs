using AutoFixture.NUnit3;
using GitObjectDb.SystemTextJson.Tools;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace GitObjectDb.SystemTextJson.Tests.Tools;
public class EnumerableFactoryTests
{
    [Test]
    [InlineAutoData(typeof(IList<string>))]
    [InlineAutoData(typeof(IEnumerable<string>))]
    [InlineAutoData(typeof(List<string>))]
    [InlineAutoData(typeof(string[]))]
    [InlineAutoData(typeof(IImmutableList<string>))]
    [InlineAutoData(typeof(ImmutableList<string>))]
    [InlineAutoData(typeof(ImmutableArray<string>))]
    public void SupportsExpectedCollectionTypes(Type collectionType, string[] values)
    {
        // Act
        var result = (IEnumerable<string>)EnumerableFactory
            .Get(typeof(string))
            .Create(collectionType, values);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Exactly(values.Length).Items);
            Assert.That(result.First(), Is.EqualTo(values[0]));
            Assert.That(result.Last(), Is.EqualTo(values.Last()));
        });
    }
}
