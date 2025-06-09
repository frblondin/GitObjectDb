using GitDotNet;
using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;
using System.Linq;

namespace Models.Software;

/// <summary>Represents an application.</summary>
[GitFolder(FolderName = "Applications")]
[HasChild(typeof(Table))]
public record Application : Node
{
    /// <summary>Gets the name of the application.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the description of the application.</summary>
    public string? Description { get; init; }
}

public static class IConnectionApplicationExtensions
{
    public static IEnumerable<Application> GetApplications(this IConnection connection, CommitEntry commit) =>
        connection.GetNodesAsync<Application>(commit).ToEnumerable().OrderBy(f => f.Id);
}
