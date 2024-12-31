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
        Assert.That(Connection.Repository.Info.IsHeadUnborn, Is.True);
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
        Assert.That(Connection.Repository.Head.Commits.ToList(), Has.Exactly(1).Items);
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
        var type = Connection.GetNodes<OrganizationType>("main").Single();
        var organization = Connection.GetNodes<Organization>("main").Single();
        Assert.That(organization, Has.Property(nameof(Organization.Type)).SameAs(type));
    }

    [Test]
    public async Task DeleteOneNodeMutationAsync()
    {
        // Arrange
        var generator = new DataGenerator(Connection, 20, 5);
        generator.CreateInitData();
        var node = Connection.GetNodes<Organization>("main").First();

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
            Assert.That(Connection.Repository.Head.Commits.ToList(), Has.Exactly(2).Items);
            Assert.That(writtenResult.GetFromPath<string>("data.deleteOrg"), Is.EqualTo(node.Path!.FilePath));
        });
    }
}
