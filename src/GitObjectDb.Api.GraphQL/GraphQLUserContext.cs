using System.Security.Claims;

namespace GitObjectDb.Api.GraphQL;

public sealed class GraphQLUserContext : Dictionary<string, object?>
{
    public ClaimsPrincipal? User { get; set; }
}
