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
using System.Text;

namespace GitObjectDb.Tests.Git.Hooks
{
    public class GitHooksTests
    {
        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void PreCommitWhenPropertyChangeGetsFired(GitHooks sut, ObjectRepository instance, Page page, LinkField field, Page newLinkedPage, Signature signature, string message, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            CommitStartedEventArgs lastEvent = null;
            sut.CommitStarted += (_, args) => lastEvent = args;

            // Act
            instance.SaveInNewRepository(signature, message, RepositoryFixture.GetRepositoryDescription(inMemoryBackend));
            var composer = new PredicateComposer()
                .And(field, f => f.Name == "modified field name" && f.PageLink == new LazyLink<Page>(newLinkedPage))
                .And(page, p => p.Name == "modified page name");
            var modified = field.With(composer);
            instance.Commit(modified.Repository, signature, message);

            // Assert
            Assert.That(lastEvent, Is.Not.Null);
            Assert.That(lastEvent.Changes, Has.Count.EqualTo(2));
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void PreCommitCancelsCommitIfRequested(GitHooks sut, ObjectRepository instance, Signature signature, string message, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            sut.CommitStarted += (_, args) => args.Cancel = true;

            // Act
            var commit = instance.SaveInNewRepository(signature, message, RepositoryFixture.GetRepositoryDescription(inMemoryBackend));

            // Assert
            Assert.That(commit, Is.Null);
        }

        [Test]
        [AutoDataCustomizations(typeof(DefaultMetadataContainerCustomization), typeof(MetadataCustomization))]
        public void PostCommitWhenPropertyChangeGetsFired(GitHooks sut, ObjectRepository instance, Page page, Signature signature, string message, InMemoryBackend inMemoryBackend)
        {
            // Arrange
            CommitCompletedEventArgs lastEvent = null;
            sut.CommitCompleted += (_, args) => lastEvent = args;

            // Act
            instance.SaveInNewRepository(signature, message, RepositoryFixture.GetRepositoryDescription(inMemoryBackend));
            var modifiedPage = page.With(p => p.Name == "modified");
            var commit = instance.Commit(modifiedPage.Repository, signature, message);

            // Assert
            Assert.That(lastEvent, Is.Not.Null);
            Assert.That(lastEvent.CommitId, Is.EqualTo(commit));
        }
    }
}
