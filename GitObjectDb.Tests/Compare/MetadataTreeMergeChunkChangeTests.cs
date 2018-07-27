using GitObjectDb.Compare;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Compare
{
    public class MetadataTreeMergeChunkChangeTests
    {
        readonly ModifiablePropertyInfo _property = new ModifiablePropertyInfo(ExpressionReflector.GetProperty<Page>(p => p.Name));

        [Test]
        [AutoDataCustomizations(typeof(JsonCustomization))]
        public void MetadataTreeMergeChunkChangePropertiesAreMatchingEntryParameterValues(string path, JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue, JToken headValue)
        {
            // Act
            var sut = new MetadataTreeMergeChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, headValue);

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
        public void MetadataTreeMergeChunkChangeShouldNotBeInConflictIfHeadValuesAreSame(string path, JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue)
        {
            // Act
            var sut = new MetadataTreeMergeChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, mergeBaseValue);

            // Assert
            Assert.That(sut.IsInConflict, Is.False);
            Assert.That(sut.MergeValue, Is.SameAs(branchValue));
        }

        [Test]
        [AutoDataCustomizations(typeof(JsonCustomization))]
        public void MetadataTreeMergeChunkChangeShouldBeInConflictIfValuesAreDifferent(string path, JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue, JToken headValue)
        {
            // Act
            var sut = new MetadataTreeMergeChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, headValue);

            // Assert
            Assert.That(sut.IsInConflict, Is.True);
            Assert.That(sut.MergeValue, Is.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(JsonCustomization))]
        public void MetadataTreeMergeChunkChangeResolveConflict(string path, JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue, JToken headValue, JToken resolvedValue)
        {
            // Act
            var sut = new MetadataTreeMergeChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, headValue);
            sut.Resolve(resolvedValue);

            // Assert
            Assert.That(sut.WasInConflict, Is.True);
            Assert.That(sut.IsInConflict, Is.False);
            Assert.That(sut.MergeValue, Is.SameAs(resolvedValue));
        }

        [Test]
        [AutoDataCustomizations(typeof(JsonCustomization))]
        public void MetadataTreeMergeChunkChangeResolveConflictOnlyOnce(string path, JObject mergeBaseNode, JObject branchNode, JObject headNode, JToken mergeBaseValue, JToken branchValue, JToken headValue, JToken resolvedValue)
        {
            // Act
            var sut = new MetadataTreeMergeChunkChange(path, mergeBaseNode, branchNode, headNode, _property, mergeBaseValue, branchValue, headValue);
            sut.Resolve(resolvedValue);
            Assert.Throws<NotSupportedException>(() => sut.Resolve(resolvedValue));
        }
    }
}
