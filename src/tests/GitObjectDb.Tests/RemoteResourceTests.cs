using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb.Tests;

public class RemoteResourceTests
{
    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void AddRemoteResourceRepository(IConnection connection, Application application, string content, string message, Signature committer)
    {
        // Arrange
        var (path, tip) = CreateResourceRepository(content, message, committer);

        // Act
        var applicationWithLinkedResources = application with
        {
            RemoteResource = new(path, tip),
        };
        connection
            .Update("main", c => c.CreateOrUpdate(applicationWithLinkedResources))
            .Commit(new(message, committer, committer));

        // Assert
        var result = connection.GetResources("main", applicationWithLinkedResources).ToList();
        Assert.That(result, Has.Exactly(1).Items);
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Path, Is.EqualTo(application.Path.CreateResourcePath("folder", "file.txt")));
            Assert.That(result[0].Embedded.ReadAsString(), Is.EqualTo(content));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void StageAndAddRemoteResourceRepository(IConnection connection, Application application, string content, string message, Signature committer)
    {
        // Arrange
        var (path, tip) = CreateResourceRepository(content, message, committer);

        // Act
        var applicationWithLinkedResources = application with
        {
            RemoteResource = new(path, tip),
        };
        connection
            .GetIndex("main", c => c.CreateOrUpdate(applicationWithLinkedResources))
            .Commit(new(message, committer, committer));

        // Assert
        var result = connection.GetResources("main", applicationWithLinkedResources).ToList();
        Assert.That(result, Has.Exactly(1).Items);
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Path, Is.EqualTo(application.Path.CreateResourcePath("folder", "file.txt")));
            Assert.That(result[0].Embedded.ReadAsString(), Is.EqualTo(content));
        });
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void ThrowIfWrongCommitId(IConnection connection, Application application, string content, string message, Signature committer)
    {
        // Arrange
        var (path, tip) = CreateResourceRepository(content, message, committer);

        // Act
        var applicationWithLinkedResources = application with
        {
            RemoteResource = new(path, tip),
        };
        var commit = connection
            .Update("main", c => c.CreateOrUpdate(applicationWithLinkedResources))
            .Commit(new(message, committer, committer));

        // Assert
        Assert.Throws<GitObjectDbException>(() => connection.GetResources("main", application with
        {
            // Replace remote commit with irrelevant commit
            RemoteResource = new(path, commit.Id.Sha),
        }).ToList());
    }

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void CannotMixEmbeddedAndRemoteResources(IConnection connection, Application application, string content, string message, Signature committer)
    {
        // Arrange
        var (path, tip) = CreateResourceRepository(content, message, committer);

        // Act
        var applicationWithLinkedResources = application with
        {
            RemoteResource = new(path, tip),
        };
        var changes = connection.Update("main", c =>
        {
            c.CreateOrUpdate(applicationWithLinkedResources);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var resource = new Resource(application,
                                        $"Path{UniqueId.CreateNew()}",
                                        $"File{UniqueId.CreateNew()}.txt",
                                        new Resource.Data(stream));
            c.CreateOrUpdate(resource);
        });

        // Assert
        Assert.Throws<GitObjectDbValidationException>(
            () => changes.Commit(new(message, committer, committer)));
    }

    private static (string Path, string Tip) CreateResourceRepository(string content, string message, Signature committer)
    {
        var path = GitObjectDbFixture.GetAvailableFolderPath();
        Repository.Init(path);

        var repo = new Repository(path);

        // Create a blob from the content stream
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var blob = repo.ObjectDatabase.CreateBlob(stream);

        // Put the blob in a tree
        var definition = new TreeDefinition();
        definition.Add("folder/file.txt", blob, Mode.NonExecutableFile);
        var tree = repo.ObjectDatabase.CreateTree(definition);

        // Create binary stream from the text
        var commit = repo.ObjectDatabase.CreateCommit(committer, committer, message, tree, repo.Commits, false);

        // Update the HEAD reference to point to the latest commit
        repo.Refs.UpdateTarget(repo.Refs.Head, commit.Id);

        return (path, commit.Id.Sha);
    }
}
