using GitDotNet;
using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;
using System.Linq;

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

    /// <summary>Gets long text value stored in separate blob.</summary>
    [StoreAsSeparateFile(Extension = "someextension")]
    public string? LongTextStoredInSeparateBlob { get; init; }
}

public static class IConnectionTableExtensions
{
    public static IEnumerable<Table> GetTables(this IConnection connection, CommitEntry commit, Application application) =>
        connection.GetNodesAsync<Table>(commit, application).ToEnumerable().OrderBy(f => f.Id);
}
