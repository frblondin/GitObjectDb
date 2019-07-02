using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Tests.Features
{
    public class IndexTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void IndexUpdateWhenPropertyIsBeingChanged(IServiceProvider serviceProvider, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, string name)
        {
            // Arrange
            repository = repository.With(c => c
                .Add(repository, r => r.Indexes, new Index(serviceProvider, UniqueId.CreateNew(), name, nameof(IModelObject.Name))));
            repository = container.AddRepository(repository, signature, message);
            ComputeKeysCalls.Clear();

            // Act
            var modified = repository.With(repository.Applications[0].Pages[1], p => p.Description, "modified description");
            container.Commit(modified.Repository, signature, message);

            // Assert
            Assert.That(ComputeKeysCalls, Has.Exactly(2).Items); // Two calls to ComputeKeys for before/after
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void IndexUpdateWhenObjectIsBeingAdded(IServiceProvider serviceProvider, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, string name)
        {
            // Arrange
            repository = repository.With(c => c
                .Add(repository, r => r.Indexes, new Index(serviceProvider, UniqueId.CreateNew(), name, nameof(IModelObject.Name))));
            repository = container.AddRepository(repository, signature, message);
            ComputeKeysCalls.Clear();
            var page = new Page(serviceProvider, UniqueId.CreateNew(), "name", "description", new LazyChildren<Field>(ImmutableList.Create(
                new Field(serviceProvider, UniqueId.CreateNew(), "name", FieldContent.Default))));

            // Act
            var modified = repository.With(c => c.Add(repository.Applications[0], app => app.Pages, page));
            container.Commit(modified.Repository, signature, message);

            // Assert
            Assert.That(ComputeKeysCalls, Has.Exactly(2).Items);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void IndexUpdateWhenObjectIsBeingRemoved(IServiceProvider serviceProvider, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, string name)
        {
            // Arrange
            repository = repository.With(c => c
                .Add(repository, r => r.Indexes, new Index(serviceProvider, UniqueId.CreateNew(), name, nameof(IModelObject.Name))));
            repository = container.AddRepository(repository, signature, message);
            ComputeKeysCalls.Clear();

            // Act
            var modified = repository.With(c => c.Remove(repository.Applications[0], app => app.Pages, repository.Applications[0].Pages[0]));
            container.Commit(modified.Repository, signature, message);

            // Assert
            var nestedCount = repository.Applications[0].Pages[0].Flatten().Count();
            Assert.That(ComputeKeysCalls, Has.Exactly(nestedCount).Items);
        }

        public static IList<IModelObject> ComputeKeysCalls { get; } = new List<IModelObject>();
    }

    [Index]
    public partial class Index
    {
        [DataMember]
        public string PropertyName { get; }

#pragma warning disable CA1801
        partial void ComputeKeys(IModelObject node, ISet<string> result)
        {
            IndexTests.ComputeKeysCalls.Add(node);

            var propertyAccessor = node.DataAccessor.ModifiableProperties.FirstOrDefault(p => p.Name == PropertyName);
            if (propertyAccessor != null)
            {
                var value = propertyAccessor.Accessor(node)?.ToString();
                result.Add(value);
            }
        }
#pragma warning restore CA1801
    }
}
