using GitObjectDb.Api.GraphQL.Tests.Assets;
using Models.Organization;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace GitObjectDb.Api.GraphQL.Assets;

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
                timeZone: ""UTC;0;(UTC) Coordinated Universal Time;Coordinated Universal Time;Coordinated Universal Time;;""
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
}
