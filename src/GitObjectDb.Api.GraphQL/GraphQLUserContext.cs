using System.Security.Claims;

namespace GitObjectDb.Api.GraphQL;

/// <summary>Represents the user context for GraphQL operations.</summary>
public sealed class GraphQLUserContext : Dictionary<string, object?>
{
    /// <summary>Gets or sets the user associated with the current context.</summary>
    public ClaimsPrincipal? User { get; set; }
}
