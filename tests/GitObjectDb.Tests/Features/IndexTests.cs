using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Transformations;
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
using System.Threading.Tasks;

namespace GitObjectDb.Tests.Features
{
    public class IndexTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task IndexUpdateWhenPropertyIsBeingChangedAsync(IServiceProvider serviceProvider, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, string name)
        {
            // Arrange
            repository = repository.With(c => c
                .Add(repository, r => r.Indexes, new Index(serviceProvider, UniqueId.CreateNew(), name, nameof(IModelObject.Name))));
            repository = await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);
            ComputeKeysCalls.Clear();

            // Act
            var modified = repository.WithAsync((await (await repository.Applications)[0].Pages)[1], p => p.Description, "modified description");
            await container.CommitAsync(modified.Repository, signature, message).ConfigureAwait(false);

            // Assert
            Assert.That(ComputeKeysCalls, Has.Count.EqualTo(2)); // Two calls to ComputeKeys for before/after
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task IndexUpdateWhenObjectIsBeingAddedAsync(IServiceProvider serviceProvider, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, string name)
        {
            // Arrange
            repository = repository.With(c => c
                .Add(repository, r => r.Indexes, new Index(serviceProvider, UniqueId.CreateNew(), name, nameof(IModelObject.Name))));
            repository = await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);
            ComputeKeysCalls.Clear();
            var page = new Page(serviceProvider, UniqueId.CreateNew(), "name", "description", new LazyChildren<Field>(ImmutableList.Create(
                new Field(serviceProvider, UniqueId.CreateNew(), "name", FieldContent.Default))));

            // Act
            var modified = await repository.WithAsync(AddPageAsync).ConfigureAwait(false);
            await container.CommitAsync(modified.Repository, signature, message).ConfigureAwait(false);

            // Assert
            Assert.That(ComputeKeysCalls, Has.Count.EqualTo(2));

            async Task<ITransformationComposer> AddPageAsync(ITransformationComposer c) => c.Add((await repository.Applications)[0], app => app.Pages, page);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public async Task IndexUpdateWhenObjectIsBeingRemovedAsync(IServiceProvider serviceProvider, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message, string name)
        {
            // Arrange
            repository = repository.With(c => c
                .Add(repository, r => r.Indexes, new Index(serviceProvider, UniqueId.CreateNew(), name, nameof(IModelObject.Name))));
            repository = await container.AddRepositoryAsync(repository, signature, message).ConfigureAwait(false);
            ComputeKeysCalls.Clear();

            // Act
            var modified = await repository.WithAsync(RemovePageAsync).ConfigureAwait(false);
            await container.CommitAsync(modified.Repository, signature, message).ConfigureAwait(false);

            // Assert
            var nestedCount = (await (await repository.Applications)[0].Pages)[0].FlattenAsync().Count();
            Assert.That(ComputeKeysCalls, Has.Count.EqualTo(nestedCount));

            async Task<ITransformationComposer> RemovePageAsync(ITransformationComposer c) => c.Remove((await repository.Applications)[0], app => app.Pages, (await (await repository.Applications)[0].Pages)[0]);
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
