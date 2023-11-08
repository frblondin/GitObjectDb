using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;

namespace Models.Software;

[GitFolder(UseNodeFolders = false)]
public record Constant : Node
{
    [StoreAsSeparateFile]
    public string Value { get; init; }
}

public static class IConnectionConstantExtensions
{
    public static IEnumerable<Constant> GetConstants(this IConnection connection, Table table, string committish = "main") =>
        connection.GetNodes<Constant>(committish, table);
}
