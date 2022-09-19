using AutoFixture;
using GitObjectDb.Tests.Assets.Tools;
using NUnit.Framework;
using System;

namespace GitObjectDb.Tests
{
    public class UniqueIdTests
    {
        [Test]
        [AutoDataCustomizations(typeof(Customization))]
        public void OperatorEquality(string sha) =>
            Assert.That(new UniqueId(sha) == new UniqueId(sha), Is.True);

        [Test]
        [AutoDataCustomizations(typeof(Customization))]
        public void OperatorInequality(string sha1, string sha2) =>
            Assert.That(new UniqueId(sha1) != new UniqueId(sha2), Is.True);

        [Test]
        [AutoDataCustomizations(typeof(Customization))]
        public void OperatorLowerThan(string sha1, string sha2) =>
            Assert.AreEqual(
                new UniqueId(sha1) < new UniqueId(sha2),
                string.CompareOrdinal(sha1, sha2) < 0);

        [Test]
        [AutoDataCustomizations(typeof(Customization))]
        public void OperatorLowerThanOrEqual(string sha1, string sha2) =>
            Assert.AreEqual(
                new UniqueId(sha1) <= new UniqueId(sha2),
                string.CompareOrdinal(sha1, sha2) <= 0);

        [Test]
        [AutoDataCustomizations(typeof(Customization))]
        public void OperatorGreaterThan(string sha1, string sha2) =>
            Assert.AreEqual(
                new UniqueId(sha1) > new UniqueId(sha2),
                string.CompareOrdinal(sha1, sha2) > 0);

        [Test]
        [AutoDataCustomizations(typeof(Customization))]
        public void OperatorGreaterThanOrEqual(string sha1, string sha2) =>
            Assert.AreEqual(
                new UniqueId(sha1) >= new UniqueId(sha2),
                string.CompareOrdinal(sha1, sha2) >= 0);

        [Test]
        [AutoDataCustomizations(typeof(Customization))]
        public void TryParseFailsWhenEmpty() =>
            Assert.That(
                UniqueId.TryParse(string.Empty, out var _),
                Is.False);

        [Test]
        [AutoDataCustomizations(typeof(Customization))]
        public void EqualsBoxed(string sha) =>
            Assert.That(new UniqueId(sha).Equals((object)new UniqueId(sha)), Is.True);

        private class Customization : ICustomization
        {
            public void Customize(IFixture fixture)
            {
                fixture.Register(() => Guid.NewGuid().ToString("N"));
            }
        }
    }
}
