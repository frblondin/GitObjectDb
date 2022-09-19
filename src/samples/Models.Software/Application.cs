using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;

namespace Models.Software;

[GitFolder(FolderName = "Applications")]
[HasChild(typeof(Table))]
public record Application : Node
{
    /// <summary>Gets or sets the name of the application.</summary>
    public string? Name { get; init; }

    public string? Description { get; init; }
}

#pragma warning disable SA1402 // File may only contain a single type
public static class IConnectionApplicationExtensions
{
    public static IEnumerable<Application> GetApplications(this IConnection connection, string? committish = null) =>
        connection.GetNodes<Application>(committish: committish);
}
