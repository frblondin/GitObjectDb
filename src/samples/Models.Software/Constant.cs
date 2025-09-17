using GitDotNet;
using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;
using System.Linq;

namespace Models.Software;

[GitFolder(UseNodeFolders = false)]
public record Constant : Node
{
    [StoreAsSeparateFile]
    public string? Value { get; init; }
}

public static class IConnectionConstantExtensions
{
    public static IEnumerable<Constant> GetConstants(this IConnection connection, CommitEntry commit, Table table) =>
        connection.GetNodesAsync<Constant>(commit, table).ToEnumerable().OrderBy(c => c.Id);
}
