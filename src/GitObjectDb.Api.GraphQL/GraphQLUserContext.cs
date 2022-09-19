using System.Security.Claims;

namespace GitObjectDb.Api.GraphQL;

public class GraphQLUserContext : Dictionary<string, object?>
{
    public ClaimsPrincipal? User { get; set; }
}
