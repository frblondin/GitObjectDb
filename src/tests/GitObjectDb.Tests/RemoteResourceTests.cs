using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using GitDotNet;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;

namespace GitObjectDb.Tests;

public class RemoteResourceTests
{
    [Test]
    public async Task AddRemoteResourceRepository()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        using var connection = fixture.Create<IConnection>();
        var provider = fixture.Create<GitConnectionProvider>();
        var application = fixture.Create<Application>();
        var content = fixture.Create<string>();
        var message = fixture.Create<string>();
        var committer = fixture.Create<Signature>();

        var (path, tip) = await CreateResourceRepositoryAsync(provider, connection, content, message, committer);

        // Act
        var applicationWithLinkedResources = application with
        {
            RemoteResource = new(path, tip.Id.ToString()),
        };
        var changes = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(applicationWithLinkedResources));
        await changes.CommitAsync(new(message, committer, committer));

        // Assert
        var mainTip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.GetResourcesAsync(mainTip, applicationWithLinkedResources).ToEnumerable().ToList();
        Assert.That(result, Has.Exactly(1).Items);
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(result[0].Path, Is.EqualTo(application.Path.CreateResourcePath("folder", "file.txt")));
            Assert.That(await result[0].Embedded.ReadAsStringAsync(), Is.EqualTo(content));
        });
    }

    [Test]
    public async Task StageAndAddRemoteResourceRepository()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        using var connection = fixture.Create<IConnection>();
        var provider = fixture.Create<GitConnectionProvider>();
        var application = fixture.Create<Application>();
        var content = fixture.Create<string>();
        var message = fixture.Create<string>();
        var committer = fixture.Create<Signature>();

        var (path, tip) = await CreateResourceRepositoryAsync(provider, connection, content, message, committer);

        // Act
        var applicationWithLinkedResources = application with
        {
            RemoteResource = new(path, tip.Id.ToString()),
        };
        var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(applicationWithLinkedResources));
        await index.CommitAsync(new(message, committer, committer));

        // Assert
        var mainTip = await connection.Repository.GetCommittishAsync("main");
        var result = connection.GetResourcesAsync(mainTip, applicationWithLinkedResources).ToEnumerable().ToList();
        Assert.That(result, Has.Exactly(1).Items);
        await Assert.MultipleAsync(async () =>
        {
            Assert.That(result[0].Path, Is.EqualTo(application.Path.CreateResourcePath("folder", "file.txt")));
            Assert.That(await result[0].Embedded.ReadAsStringAsync(), Is.EqualTo(content));
        });
    }

    private static async Task<(string Path, CommitEntry Tip)> CreateResourceRepositoryAsync(
        GitConnectionProvider provider, IConnection connection, string content, string message, Signature committer)
    {
        var path = Path.Combine(connection.Repository.Info.Path, Guid.NewGuid().ToString());
        GitConnection.Create(path);

        var repo = provider(path);

        // Create a blob from the content stream
        var commit = await repo.CommitAsync("main",
            c => c.AddOrUpdate("folder/file.txt", Encoding.UTF8.GetBytes(content)),
            repo.CreateCommit(message, [], committer, committer));

        return (path, commit);
    }
}