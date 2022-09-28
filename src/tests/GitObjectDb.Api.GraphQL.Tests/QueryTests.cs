using GitObjectDb.Api.GraphQL.Tests.Assets;
using GraphQL;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitObjectDb.Api.GraphQL.Assets;

public class QueryTests : QueryTestBase
{
    [Test]
    public async Task ReadData()
    {
        // Arrange
        var query = @"
            {
              organizations(committish: ""main"") {
                ...OrganizationFields
                children {
                  path
                  ... on Organization {
                    ...OrganizationFields
                    children {
                      ... on Organization {
                        ...OrganizationFields
                        children {
                          ... on Organization {
                            ...OrganizationFields
                          }
                        }
                      }
                    }
                  }
                }
              }
            }

            fragment OrganizationFields on Organization {
              label
              id
            }";

        // Act
        var result = await AssertQuerySuccessAsync(query);
        var writtenResult = JsonDocument.Parse(Serializer.Serialize(result)).RootElement;

        // Assert
        var northAmerica = writtenResult.GetFromPath<JsonElement>(
            @"data.organizations[item.GetProperty(""id"").GetString() == ""northAmerica""]");
        var children = northAmerica.GetFromPath<JsonElement>(@"children");
        Assert.Multiple(() =>
        {
            Assert.That(northAmerica.GetFromPath<string>(@"label"), Is.EqualTo("North America"));
            Assert.That(children.GetArrayLength(), Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ReadDeltaSincePreviousCommit()
    {
        // Arrange
        var query = @"
            {
              organizationsDelta(start: ""main~1"", end: ""main"") {
                updatedAt
                deleted
                old {
                  graphicalOrganizationStructureId
                }
                new {
                  graphicalOrganizationStructureId
                }
              }
            }";

        // Act
        var result = await AssertQuerySuccessAsync(query);
        var writtenResult = JsonDocument.Parse(Serializer.Serialize(result)).RootElement;

        // Assert
        var old = writtenResult.GetFromPath<string>("data.organizationsDelta[0].old.graphicalOrganizationStructureId");
        var @new = writtenResult.GetFromPath<string>("data.organizationsDelta[0].new.graphicalOrganizationStructureId");
        Assert.Multiple(() =>
        {
            Assert.That(old, Is.Null);
            Assert.That(@new, Is.EqualTo("hahaha"));
        });
    }

    [SetUp]
    public async Task AddContent()
    {
        await Executer.ExecuteAsync(options =>
        {
            options.Schema = Schema;
            options.Query = Tests.Resource.CreateData;
            options.UserContext = new Dictionary<string, object?>();
            options.RequestServices = ServiceProvider;
        }).ConfigureAwait(false);
    }
}
