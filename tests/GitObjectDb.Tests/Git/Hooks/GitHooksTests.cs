using GitObjectDb.Git;
using GitObjectDb.Git.Hooks;
using GitObjectDb.Models;
using GitObjectDb.Reflection;
using GitObjectDb.Tests.Assets.Customizations;
using GitObjectDb.Tests.Assets.Models;
using GitObjectDb.Tests.Assets.Utils;
using GitObjectDb.Tests.Git.Backends;
using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests.Git.Hooks
{
    public class GitHooksTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PreCommitWhenPropertyChangeGetsFired(GitHooks sut, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Arrange
            repository = container.AddRepository(repository, signature, message);
            CommitStartedEventArgs lastEvent = null;
            sut.CommitStarted += (_, args) => lastEvent = args;
            var field = repository.Flatten().OfType<Field>().First(
                f => f.Content.MatchOrDefault(matchLink: l => true));

            // Act
            var page = repository.Applications[0].Pages[1];
            var newLinkedPage = repository.Applications[1].Pages[2];
            var modified = repository.With(c => c
                .Update(field, f => f.Name, "modified field name")
                .Update(field, f => f.Content, FieldContent.NewLink(new FieldLinkContent(new LazyLink<Page>(container, newLinkedPage))))
                .Update(page, p => p.Name, "modified page name"));
            container.Commit(modified.Repository, signature, message);

            // Assert
            Assert.That(lastEvent, Is.Not.Null);
            Assert.That(lastEvent.Changes, Has.Count.EqualTo(2));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PreCommitCancelsCommitIfRequested(GitHooks sut, ObjectRepository instance, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Arrange
            sut.CommitStarted += (_, args) => args.Cancel = true;

            // Act
            var update = container.AddRepository(instance, signature, message);

            // Assert
            Assert.That(update, Is.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PostCommitWhenPropertyChangeGetsFired(GitHooks sut, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> container, Signature signature, string message)
        {
            // Arrange
            CommitCompletedEventArgs lastEvent = null;
            sut.CommitCompleted += (_, args) => lastEvent = args;

            // Act
            repository = container.AddRepository(repository, signature, message);
            var modifiedPage = repository.With(repository.Applications[0].Pages[0], p => p.Name, "modified");
            var commit = container.Commit(modifiedPage.Repository, signature, message);

            // Assert
            Assert.That(lastEvent, Is.Not.Null);
            Assert.That(lastEvent.CommitId, Is.EqualTo(commit.CommitId));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PreMergeGetsFiredWhenPulling(GitHooks sut, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> origin, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange - Create origin and local repositories
            repository = origin.AddRepository(repository, signature, message);
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            var clientContainer = containerFactory.Create<ObjectRepository>(tempPath);
            clientContainer.Clone(origin.Repositories.Single().RepositoryDescription.Path);

            // Arrange - Commit change on origin
            var change = repository.With(repository.Applications[0].Pages[0], p => p.Description, "foo");
            origin.Commit(change.Repository, signature, message);

            // Arrange - suscribe to hook
            MergeStartedEventArgs lastEvent = null;
            sut.MergeStarted += (_, args) => lastEvent = args;

            // Act - Pull commit from origin
            var pullResult = clientContainer.Pull(clientContainer.Repositories.Single().Id);
            var result = pullResult.Apply(signature);

            // Assert
            Assert.That(lastEvent, Is.Not.Null);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PreMergeCancelsPullWhenRequested(GitHooks sut, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> origin, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange - Create origin and local repositories
            repository = origin.AddRepository(repository, signature, message);
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            var clientContainer = containerFactory.Create<ObjectRepository>(tempPath);
            clientContainer.Clone(origin.Repositories.Single().RepositoryDescription.Path);

            // Arrange - Commit change on origin
            var change = repository.With(repository.Applications[0].Pages[0], p => p.Description, "foo");
            origin.Commit(change.Repository, signature, message);

            // Arrange - suscribe to hook
            sut.MergeStarted += (_, args) => args.Cancel = true;

            // Act - Pull commit from origin
            var pullResult = clientContainer.Pull(clientContainer.Repositories.Single().Id);
            var result = pullResult.Apply(signature);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultContainerCustomization), typeof(ModelCustomization))]
        public void PostMergeGetsFiredWhenPulling(GitHooks sut, ObjectRepository repository, IObjectRepositoryContainer<ObjectRepository> origin, IObjectRepositoryContainerFactory containerFactory, Signature signature, string message)
        {
            // Arrange - Create origin and local repositories
            repository = origin.AddRepository(repository, signature, message);
            var tempPath = RepositoryFixture.GetAvailableFolderPath();
            var clientContainer = containerFactory.Create<ObjectRepository>(tempPath);
            clientContainer.Clone(origin.Repositories.Single().RepositoryDescription.Path);

            // Arrange - Commit change on origin
            var change = repository.With(repository.Applications[0].Pages[0], p => p.Description, "foo");
            origin.Commit(change.Repository, signature, message);

            // Arrange - suscribe to hook
            MergeCompletedEventArgs lastEvent = null;
            sut.MergeCompleted += (_, args) => lastEvent = args;

            // Act - Pull commit from origin
            var pullResult = clientContainer.Pull(clientContainer.Repositories.Single().Id);
            pullResult.Apply(signature);

            // Assert
            Assert.That(lastEvent, Is.Not.Null);
        }
    }
}
