using GitObjectDb.Api.GraphQL.Tests.Assets;
using Models.Organization;
using Namotion.Reflection;
using NUnit.Framework;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitObjectDb.Api.GraphQL.Tests;

public class MutationTests : QueryTestBase
{
    [Test]
    public async Task CreateOneUncommittedMutationAsync()
    {
        // Arrange
        var query = @"
            mutation {
              checkout(branch: ""main"")
              createOrganizationType(node: {
                id: ""site"", label: ""Site""
              })
            }";
        var expected = @"
            {
              ""checkout"": ""main"",
              ""createOrganizationType"": ""Types/site.json""
            }";

        // Act
        await AssertQuerySuccessAsync(query, expected);

        // Assert
        Assert.That(Connection.Repository.Branches.TryGet("main", out _), Is.False);
    }

    [Test]
    public async Task CreateOneCommittedMutationAsync()
    {
        // Arrange
        var query = @"
            mutation {
              checkout(branch: ""main"")
              createSite: createOrganizationType(node: {
                id: ""site"", label: ""Site""
              })
              initialCommit: commit(
                message: ""Initial commit"",
                author: ""Me"",
                email: ""me@myself.com""
              )
            }";
        var expected = @$"
            {{
              ""checkout"": ""main"",
              ""createSite"": ""Types/site.json"",
              ""initialCommit"": ""{Commit}""
            }}";

        // Act
        await AssertQuerySuccessAsync(query, expected);

        // Assert
        Assert.That(await Connection.Repository.Branches["main"].GetTipAsync(), Is.Not.Null);
    }

    [Test]
    public async Task CreateLinkMutationAsync()
    {
        // Arrange
        var query = @"
            mutation {
              checkout(branch: ""main"")
              createSite: createOrganizationType(node: {
                id: ""site"", label: ""Site""
              })
              siteX: createOrganization(node: {
                id: ""siteX""
                label: ""Site X""
                type: ""Types/site.json""
                timeZone: Etc_GMT_plus_1
              })
              initialCommit: commit(
                message: ""Initial commit"",
                author: ""Me"",
                email: ""me@myself.com""
              )
            }";
        var expected = @$"
            {{
              ""checkout"": ""main"",
              ""createSite"": ""Types/site.json"",
              ""siteX"": ""Organizations/siteX/siteX.json"",
              ""initialCommit"": ""{Commit}""
            }}";

        // Act
        await AssertQuerySuccessAsync(query, expected);

        // Assert
        var tip = await Connection.Repository.GetCommittishAsync("main");
        var type = Connection.GetNodesAsync<OrganizationType>(tip).ToEnumerable().Single();
        var organization = Connection.GetNodesAsync<Organization>(tip).ToEnumerable().Single();
        Assert.That(organization, Has.Property(nameof(Organization.Type)).SameAs(type));
    }

    [Test]
    public async Task DeleteOneNodeMutationAsync()
    {
        // Arrange
        var generator = new DataGenerator(Connection, 20, 5);
        await generator.CreateInitDataAsync();
        var tip = await Connection.Repository.GetCommittishAsync("main");
        var node = Connection.GetNodesAsync<Organization>(tip).ToEnumerable().First();

        // Act
        var result = await AssertQuerySuccessAsync(@$"
            mutation {{
              checkout(branch: ""main"")
              deleteOrg: deleteNode(path: ""{node.Path}"")
              deleteCommit: commit(
                message: ""Delete"",
                author: ""Me"",
                email: ""me@myself.com""
              )
            }}");
        var writtenResult = JsonDocument.Parse(Serializer.Serialize(result)).RootElement;

        // Assert
        Assert.Multiple(() =>
        {
            var commits = Connection.Repository.GetLogAsync("main").ToEnumerable().ToList();
            Assert.That(commits, Has.Exactly(2).Items);
            Assert.That(writtenResult.GetFromPath<string>("data.deleteOrg"), Is.EqualTo(node.Path!.FilePath));
        });
    }
}
