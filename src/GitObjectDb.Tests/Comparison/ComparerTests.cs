using GitObjectDb.Comparison;
using GitObjectDb.Model;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;

namespace GitObjectDb.Tests.Comparison
{
    public class ComparerTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
        public void EmbeddedResourceChangesGetDetected(IComparer sut, UniqueId id, string oldValue, string newValue)
        {
            // Arrange
            var model = new ConventionBaseModelBuilder().RegisterType<SomeNode>().Build();

            // Act
            var result = sut.Compare(
                new SomeNode { Id = id, EmbeddedResource = oldValue },
                new SomeNode { Id = id, EmbeddedResource = newValue },
                model.DefaultComparisonPolicy);

            // Assert
            Assert.That(result.Differences, Has.Exactly(1).Items);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization))]
        public void EmbeddedResourceUnchangedGetIgnored(IComparer sut, UniqueId id, string value)
        {
            // Arrange
            var model = new ConventionBaseModelBuilder().RegisterType<SomeNode>().Build();

            // Act
            var result = sut.Compare(
                new SomeNode { Id = id, EmbeddedResource = value },
                new SomeNode { Id = id, EmbeddedResource = value },
                model.DefaultComparisonPolicy);

            // Assert
            Assert.That(result.Differences, Is.Empty);
        }

        private record SomeNode : Node
        {
            public string Value { get; set; }
        }
    }
}
