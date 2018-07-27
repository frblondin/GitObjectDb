using AutoFixture;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace GitObjectDb.Tests.Models
{
    public class LazyChildrenTests
    {
        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenReturnUnchangedEntryChildrenIfProvided(IList<IMetadataObject> values, IMetadataObject parent)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            Assert.That(sut.Parent, Is.SameAs(parent));
            Assert.That(sut.AreChildrenLoaded, Is.True);
            Assert.That(sut.Count, Is.EqualTo(values.Count));
            Assert.That(sut, Is.EquivalentTo(values));
            Assert.That(sut.ForceVisit, Is.False);
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenReturnUnchangedLambdaReturnedValues(IList<IMetadataObject> values, IMetadataObject parent)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(_ => values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            Assert.That(sut.Parent, Is.SameAs(parent));
            Assert.That(sut.AreChildrenLoaded, Is.False);
            Assert.That(sut.Count, Is.EqualTo(values.Count));
            Assert.That(sut, Is.EquivalentTo(values));
            Assert.That(sut.ForceVisit, Is.False);
            Assert.That(sut.AreChildrenLoaded, Is.True);
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenThrowsErrorIfLambdaReturnsNull(IMetadataObject parent)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(_ => null);
            sut.AttachToParent(parent);

            // Assert
            Assert.Throws<NotSupportedException>(() => sut.GetEnumerator());
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenForceVisitIsFalseByDefault(IList<IMetadataObject> values, IMetadataObject parent)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            Assert.That(sut.ForceVisit, Is.False);
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenChildrenAccessThrowErrorIfNoParentAttached(IList<IMetadataObject> values)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());

            // Assert
            Assert.Throws<NotSupportedException>(() => sut.GetEnumerator());
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenThrowsErrorWhenAddGetsCalledDirectly(IList<IMetadataObject> values, IMetadataObject parent, IMetadataObject child)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            Assert.Throws<NotSupportedException>(() => sut.Add(child));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenThrowsErrorWhenDeleteGetsCalledDirectly(IList<IMetadataObject> values, IMetadataObject parent)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            Assert.Throws<NotSupportedException>(() => sut.Delete(values[0]));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenAttachToSameParentTwiceIsIgnored(IList<IMetadataObject> values, IMetadataObject parent)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            sut.AttachToParent(parent); // No error
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenAttachToOnlyOneParent(IList<IMetadataObject> values, IMetadataObject parent, IMetadataObject otherParent)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            Assert.Throws<NotSupportedException>(() => sut.AttachToParent(otherParent));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenAttachToNullParentThrowsException(IList<IMetadataObject> values)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());

            // Assert
            Assert.Throws<ArgumentNullException>(() => sut.AttachToParent(null));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenCloneCallsUpdateLambda(IFixture fixture, IList<IMetadataObject> values, IMetadataObject parent)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());
            sut.AttachToParent(parent);
            var newValues = new List<IMetadataObject>();
            var clone = (LazyChildren<IMetadataObject>)sut.Clone(true, _ =>
            {
                var o = fixture.Create<IMetadataObject>();
                newValues.Add(o);
                return o;
            });

            // Assert
            Assert.That(clone.Parent, Is.Null);
            Assert.That(clone.ForceVisit, Is.True);
            clone.AttachToParent(parent);
            Assert.That(clone.Count, Is.EqualTo(values.Count));
            Assert.That(clone, Is.EquivalentTo(newValues));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenCloneContainsAddedValues(IList<IMetadataObject> values, IMetadataObject parent, IMetadataObject added)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());
            sut.AttachToParent(parent);
            var clone = (LazyChildren<IMetadataObject>)sut.Clone(true, o => o, added: new[] { added });

            // Assert
            clone.AttachToParent(parent);
            Assert.That(clone.Count, Is.EqualTo(values.Count + 1));
            Assert.That(clone, Does.Contain(added));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenCloneDoesNotContainDeletedValues(IList<IMetadataObject> values, IMetadataObject parent)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());
            sut.AttachToParent(parent);
            var clone = (LazyChildren<IMetadataObject>)sut.Clone(true, o => o, deleted: new[] { values[0] });

            // Assert
            clone.AttachToParent(parent);
            Assert.That(clone.Count, Is.EqualTo(values.Count - 1));
            Assert.That(clone, Does.Not.Contain(values[0]));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenCloneNullLambdaThrowsException(IList<IMetadataObject> values)
        {
            // Act
            var sut = new LazyChildren<IMetadataObject>(values.ToImmutableList());

            // Assert
            Assert.Throws<ArgumentNullException>(() => sut.Clone(true, null));
        }
    }
}
