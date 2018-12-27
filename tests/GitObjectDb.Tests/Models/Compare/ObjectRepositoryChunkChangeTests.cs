using GitObjectDb.Models;
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
        private static readonly ModifiablePropertyInfo _nameProperty = new ModifiablePropertyInfo(ExpressionReflector.GetProperty<Field>(p => p.Name));

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ObjectRepositoryMergeChunkChangePropertiesAreMatchingEntryParameterValues(Page page)
        {
            // Arrange
            var path = RepositoryFixture.GetAvailableFolderPath();
            Field mergeBaseNode = page.Fields[0], branchNode = page.Fields[1], headNode = page.Fields[2];

            // Act
            var sut = new ObjectRepositoryChunkChange(path, _nameProperty, CreateChunk(mergeBaseNode), CreateChunk(branchNode), CreateChunk(headNode), true);

            // Assert
            Assert.That(sut.Path, Is.SameAs(path));
            Assert.That(sut.Property, Is.SameAs(_nameProperty));
            Assert.That(sut.Ancestor.Object, Is.SameAs(mergeBaseNode));
            Assert.That(sut.Theirs.Object, Is.SameAs(branchNode));
            Assert.That(sut.Ours.Object, Is.SameAs(headNode));
            Assert.That(sut.Ancestor.Property, Is.SameAs(_nameProperty));
            Assert.That(sut.Theirs.Property, Is.SameAs(_nameProperty));
            Assert.That(sut.Ours.Property, Is.SameAs(_nameProperty));
            Assert.That(sut.Ancestor.Value, Is.SameAs(mergeBaseNode.Name));
            Assert.That(sut.Theirs.Value, Is.SameAs(branchNode.Name));
            Assert.That(sut.Ours.Value, Is.SameAs(headNode.Name));
            Assert.That(sut.WasInConflict, Is.True);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ObjectRepositoryMergeChunkChangeResolveConflict(Page page, string resolvedValue)
        {
            // Arrange
            var path = RepositoryFixture.GetAvailableFolderPath();
            Field mergeBaseNode = page.Fields[0], branchNode = page.Fields[1], headNode = page.Fields[2];

            // Act
            var sut = new ObjectRepositoryChunkChange(path, _nameProperty, CreateChunk(mergeBaseNode), CreateChunk(branchNode), CreateChunk(headNode), true);
            sut.Resolve(resolvedValue);

            // Assert
            Assert.That(sut.WasInConflict, Is.True);
            Assert.That(sut.IsInConflict, Is.False);
            Assert.That(sut.MergeValue, Is.SameAs(resolvedValue));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void ObjectRepositoryMergeChunkChangeResolveConflictOnlyOnce(Page page, string resolvedValue)
        {
            // Arrange
            var path = RepositoryFixture.GetAvailableFolderPath();
            Field mergeBaseNode = page.Fields[0], branchNode = page.Fields[1], headNode = page.Fields[2];

            // Act
            var sut = new ObjectRepositoryChunkChange(path, _nameProperty, CreateChunk(mergeBaseNode), CreateChunk(branchNode), CreateChunk(headNode), true);
            sut.Resolve(resolvedValue);
            Assert.Throws<GitObjectDbException>(() => sut.Resolve(resolvedValue));
        }

        private static ObjectRepositoryChunk CreateChunk(IModelObject @object) =>
            new ObjectRepositoryChunk(@object, _nameProperty, @object.Name);
    }
}
