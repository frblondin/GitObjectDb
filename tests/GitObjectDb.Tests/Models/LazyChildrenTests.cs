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
        public void LazyChildrenReturnUnchangedEntryChildrenIfProvided(IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());
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
        public void LazyChildrenReturnUnchangedLambdaReturnedValues(IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(_ => values.ToImmutableList());
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
        public void LazyChildrenThrowsErrorIfLambdaReturnsNull(IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(_ => null);
            sut.AttachToParent(parent);

            // Assert
            Assert.Throws<GitObjectDbException>(() => sut.GetEnumerator());
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenForceVisitIsFalseByDefault(IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            Assert.That(sut.ForceVisit, Is.False);
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenChildrenAccessThrowErrorIfNoParentAttached(IList<IModelObject> values)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());

            // Assert
            Assert.Throws<GitObjectDbException>(() => sut.GetEnumerator());
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenAttachToSameParentTwiceIsIgnored(IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            sut.AttachToParent(parent); // No error
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenAttachToOnlyOneParent(IList<IModelObject> values, IModelObject parent, IModelObject otherParent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());
            sut.AttachToParent(parent);

            // Assert
            Assert.Throws<GitObjectDbException>(() => sut.AttachToParent(otherParent));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenAttachToNullParentThrowsException(IList<IModelObject> values)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());

            // Assert
            Assert.Throws<ArgumentNullException>(() => sut.AttachToParent(null));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenCloneCallsUpdateLambda(IFixture fixture, IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());
            sut.AttachToParent(parent);
            var newValues = new List<IModelObject>();
            var clone = (LazyChildren<IModelObject>)sut.Clone(true, _ =>
            {
                var o = fixture.Create<IModelObject>();
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
        public void LazyChildrenCloneContainsAddedValues(IList<IModelObject> values, IModelObject parent, IModelObject added)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());
            sut.AttachToParent(parent);
            var clone = (LazyChildren<IModelObject>)sut.Clone(true, o => o, added: new[] { added });

            // Assert
            clone.AttachToParent(parent);
            Assert.That(clone.Count, Is.EqualTo(values.Count + 1));
            Assert.That(clone, Does.Contain(added));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenCloneDoesNotContainDeletedValues(IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());
            sut.AttachToParent(parent);
            var clone = (LazyChildren<IModelObject>)sut.Clone(true, o => o, deleted: new[] { values[0] });

            // Assert
            clone.AttachToParent(parent);
            Assert.That(clone.Count, Is.EqualTo(values.Count - 1));
            Assert.That(clone, Does.Not.Contain(values[0]));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenCloneNullLambdaThrowsException(IList<IModelObject> values)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList());

            // Assert
            Assert.Throws<ArgumentNullException>(() => sut.Clone(true, null));
        }
    }
}
