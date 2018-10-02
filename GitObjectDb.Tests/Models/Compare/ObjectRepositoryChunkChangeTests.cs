using GitObjectDb.Models.Compare;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Models.Compare
{
    public class ObjectRepositoryChunkChangeTests
    {
        readonly ModifiablePropertyInfo _property = new ModifiablePropertyInfo(ExpressionReflector.GetProperty<Page>(p => p.Name));

        [Test]
        [AutoDataCustomizations(typeof(JsonCustomization))]
        public void ObjectRepositoryMergeChunkChangePropertiesAreMatchingEntryParameterValues(JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue, JToken headValue)
        {
            // Arrange
            var path = RepositoryFixture.GetAvailableFolderPath();

            // Act
            var sut = new ObjectRepositoryChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, headValue);

            // Assert
            Assert.That(sut.Path, Is.SameAs(path));
            Assert.That(sut.MergeBaseNode, Is.SameAs(mergeBaseNode));
            Assert.That(sut.BranchNode, Is.SameAs(branchNode));
            Assert.That(sut.HeadNode, Is.SameAs(headNode));
            Assert.That(sut.Property, Is.SameAs(_property));
            Assert.That(sut.MergeBaseValue, Is.SameAs(mergeBaseValue));
            Assert.That(sut.BranchValue, Is.SameAs(branchValue));
            Assert.That(sut.HeadValue, Is.SameAs(headValue));
        }

        [Test]
        [AutoDataCustomizations(typeof(JsonCustomization))]
        public void ObjectRepositoryMergeChunkChangeShouldNotBeInConflictIfHeadValuesAreSame(JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue)
        {
            // Arrange
            var path = RepositoryFixture.GetAvailableFolderPath();

            // Act
            var sut = new ObjectRepositoryChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, mergeBaseValue);

            // Assert
            Assert.That(sut.IsInConflict, Is.False);
            Assert.That(sut.MergeValue, Is.SameAs(branchValue));
        }

        [Test]
        [AutoDataCustomizations(typeof(JsonCustomization))]
        public void ObjectRepositoryMergeChunkChangeShouldBeInConflictIfValuesAreDifferent(JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue, JToken headValue)
        {
            // Arrange
            var path = RepositoryFixture.GetAvailableFolderPath();

            // Act
            var sut = new ObjectRepositoryChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, headValue);

            // Assert
            Assert.That(sut.IsInConflict, Is.True);
            Assert.That(sut.MergeValue, Is.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(JsonCustomization))]
        public void ObjectRepositoryMergeChunkChangeResolveConflict(JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue, JToken headValue, JToken resolvedValue)
        {
            // Arrange
            var path = RepositoryFixture.GetAvailableFolderPath();

            // Act
            var sut = new ObjectRepositoryChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, headValue);
            sut.Resolve(resolvedValue);

            // Assert
            Assert.That(sut.WasInConflict, Is.True);
            Assert.That(sut.IsInConflict, Is.False);
            Assert.That(sut.MergeValue, Is.SameAs(resolvedValue));
        }

        [Test]
        [AutoDataCustomizations(typeof(JsonCustomization))]
        public void ObjectRepositoryMergeChunkChangeResolveConflictOnlyOnce(JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue, JToken headValue, JToken resolvedValue)
        {
            // Arrange
            var path = RepositoryFixture.GetAvailableFolderPath();

            // Act
            var sut = new ObjectRepositoryChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, headValue);
            sut.Resolve(resolvedValue);
            Assert.Throws<GitObjectDbException>(() => sut.Resolve(resolvedValue));
        }
    }
}
