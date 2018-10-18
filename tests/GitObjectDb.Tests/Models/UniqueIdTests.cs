using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Utils;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public class UniqueIdTests
    {
        [Test]
        public void CheckUniqueIdUniqueness()
        {
            // Act
            var values = Enumerable.Range(0, 1000).Select(_ => UniqueId.CreateNew()).ToList();

            // Assert
            Assert.That(values.Distinct().Count(), Is.EqualTo(values.Count));
        }

        [Test]
        public void TryParseWorksIfContainsExpectedCharCount()
        {
            // Arrange
            var sha = new string('a', UniqueId.ShaLength);

            // Act
            Assert.True(UniqueId.TryParse(sha, out var id));

            // Assert
            Assert.That(id.ToString(), Is.EqualTo(sha));
        }

        [Test]
        public void TryParseFailsIfContainsUnexpectedCharCount()
        {
            // Arrange
            var sha = new string('a', UniqueId.ShaLength + 1);

            // Act
            Assert.False(UniqueId.TryParse(sha, out var _));
        }

        [Test]
        public void TryParseFailsIfContainsUnexpectedChars()
        {
            // Assert
            Assert.False(UniqueId.TryParse(new string(' ', UniqueId.ShaLength), out var _));
            Assert.False(UniqueId.TryParse(new string('é', UniqueId.ShaLength), out var _));
            Assert.False(UniqueId.TryParse(new string('.', UniqueId.ShaLength), out var _));
        }

        [Test]
        public void TryParseFailsIfNull()
        {
            // Act
            Assert.False(UniqueId.TryParse(null, out var _));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void EqualityVariousCases(UniqueId id, UniqueId other)
        {
            // Arrange
            var clone = new UniqueId(id.ToString());
            var empty = default(UniqueId);

            // Assert
            Assert.True(id.Equals(clone));
            Assert.True(id.Equals((object)clone));
            Assert.True(empty.Equals(empty));
            Assert.True(empty.Equals((object)empty));
            Assert.False(id.Equals(other));
            Assert.False(id.Equals((object)other));
            Assert.False(id.Equals(empty));
            Assert.False(id.Equals((object)empty));
            Assert.False(empty.Equals(id));
            Assert.False(empty.Equals((object)id));
            Assert.False(id.Equals(null));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void EqualityOperatorVariousCases(UniqueId id, UniqueId other)
        {
            // Arrange
            var clone = new UniqueId(id.ToString());
            var empty = default(UniqueId);

            // Assert
            Assert.True(id == clone);
            Assert.True(empty == default);
            Assert.False(id == other);
            Assert.False(id == empty);
            Assert.False(empty == id);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void InequalityOperatorVariousCases(UniqueId id, UniqueId other)
        {
            // Arrange
            var clone = new UniqueId(id.ToString());
            var empty = default(UniqueId);

            // Assert
            Assert.False(id != clone);
            Assert.False(empty != default);
            Assert.True(id != other);
            Assert.True(id != empty);
            Assert.True(empty != id);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void Compare(UniqueId a, UniqueId b)
        {
            // Assert
            Assert.That(a.CompareTo(b), Is.EqualTo(StringComparer.Ordinal.Compare(a.ToString(), b.ToString())));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void GetHashCode(UniqueId id)
        {
            // Assert
            Assert.That(id.GetHashCode(), Is.EqualTo(StringComparer.Ordinal.GetHashCode(id.ToString())));
            Assert.That(default(UniqueId).GetHashCode(), Is.EqualTo(0));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void JsonSerialization(UniqueId id)
        {
            // Act
            var json = JsonConvert.SerializeObject(id);

            // Assert
            Assert.That(json, Is.EqualTo($@"""{id.ToString()}"""));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void JsonDeserialization(UniqueId id)
        {
            // Act
            var json = $@"""{id.ToString()}""";

            // Assert
            Assert.That(JsonConvert.DeserializeObject<UniqueId>(json), Is.EqualTo(id));
        }
    }
}
