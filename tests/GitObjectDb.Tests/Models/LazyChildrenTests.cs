using AutoFixture;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Models
{
    public class LazyChildrenTests
    {
        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public async Task LazyChildrenReturnUnchangedEntryChildrenIfProvidedAsync(IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = (new LazyChildren<IModelObject>(values.ToImmutableList())).AttachToParent(parent);

            // Assert
            Assert.That(sut.Parent, Is.SameAs(parent));
            Assert.That(sut.IsStarted, Is.True);
            var awaited = await sut;
            Assert.That(awaited.Count, Is.EqualTo(values.Count));
            Assert.That(awaited, Is.EquivalentTo(values));
            Assert.That(sut.ForceVisit, Is.False);
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public async Task LazyChildrenReturnUnchangedLambdaReturnedValuesAsync(IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(
                _ => Task.FromResult<IImmutableList<IModelObject>>(values.ToImmutableList()))
                .AttachToParent(parent);

            // Assert
            Assert.That(sut.Parent, Is.SameAs(parent));
            Assert.That(sut.IsStarted, Is.False);
            var awaited = await sut;
            Assert.That(awaited.Count, Is.EqualTo(values.Count));
            Assert.That(awaited, Is.EquivalentTo(values));
            Assert.That(sut.ForceVisit, Is.False);
            Assert.That(sut.IsStarted, Is.True);
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public void LazyChildrenThrowsErrorIfLambdaReturnsNull(IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(_ => null).AttachToParent(parent);

            // Assert
            Assert.Throws<GitObjectDbException>(async () => (await sut).GetEnumerator());
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
            Assert.Throws<GitObjectDbException>(async () => (await sut).GetEnumerator());
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
        public void LazyChildrenCloneHasNoParent(IFixture fixture, IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList()).AttachToParent(parent);
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
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public async Task LazyChildrenCloneCallsUpdateLambdaAsync(IFixture fixture, IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList()).AttachToParent(parent);
            var newValues = new List<IModelObject>();
            var clone = ((LazyChildren<IModelObject>)sut.Clone(true, _ =>
            {
                var o = fixture.Create<IModelObject>();
                newValues.Add(o);
                return o;
            })).AttachToParent(parent);

            // Assert
            var awaited = await clone;
            Assert.That(awaited.Count, Is.EqualTo(values.Count));
            Assert.That(awaited, Is.EquivalentTo(newValues));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public async Task LazyChildrenCloneContainsAddedValuesAsync(IList<IModelObject> values, IModelObject parent, IModelObject added)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList()).AttachToParent(parent);
            var clone = ((LazyChildren<IModelObject>)sut.Clone(true, o => o, added: new[] { added })).AttachToParent(parent);

            // Assert
            var awaited = await clone;
            Assert.That(awaited.Count, Is.EqualTo(values.Count + 1));
            Assert.That(awaited, Does.Contain(added));
        }

        [Test]
        [AutoDataCustomizations(typeof(NSubstituteForAbstractTypesCustomization))]
        public async Task LazyChildrenCloneDoesNotContainDeletedValuesAsync(IList<IModelObject> values, IModelObject parent)
        {
            // Act
            var sut = new LazyChildren<IModelObject>(values.ToImmutableList()).AttachToParent(parent);
            var clone = ((LazyChildren<IModelObject>)sut.Clone(true, o => o, deleted: new[] { values[0] })).AttachToParent(parent);

            // Assert
            var awaited = await clone;
            Assert.That(awaited.Count, Is.EqualTo(values.Count - 1));
            Assert.That(awaited, Has.No.EqualTo(values[0]));
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
