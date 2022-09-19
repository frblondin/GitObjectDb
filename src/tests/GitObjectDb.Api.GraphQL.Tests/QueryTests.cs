using GitObjectDb.Api.GraphQL.Tests.Assets;
using GraphQL;
using Microsoft.AspNetCore.Rewrite;
using Models.Organization;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitObjectDb.Api.GraphQL.Assets;

public class QueryTests : QueryTestBase
{
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

    [Test]
    public async Task ReadDeltaSincePreviousCommit()
    {
        // Arrange
        var query = @"
            {
              organizationsDelta(start: ""HEAD~1"") {
                updatedAt
                deleted
                old {
                  label
                  graphicalOrganizationStructureId
                  timeZone
                  embeddedResource
                  path
                }
                new {
                  label
                  graphicalOrganizationStructureId
                  timeZone
                  embeddedResource
                  path
                }
              }
            }";

        // Act
        var result = await AssertQuerySuccessAsync(query);
        var writtenResult = JsonDocument.Parse(Serializer.Serialize(result));

        // Assert
    }
}
