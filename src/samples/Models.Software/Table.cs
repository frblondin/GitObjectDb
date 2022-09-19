using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;

namespace Models.Software;

/// <summary>Represents a table containing several fields.</summary>
[GitFolder(FolderName = "Pages")]
[HasChild(typeof(Field))]
[HasChild(typeof(Constant))]
public record Table : Node
{
    /// <summary>Gets the name of the table.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the description of the table.</summary>
    public string? Description { get; init; }
}

public static class IConnectionTableExtensions
{
    public static IEnumerable<Table> GetTables(this IConnection connection, Application application, string? committish = null) =>
        connection.GetNodes<Table>(application, committish);
}
