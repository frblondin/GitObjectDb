using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;

namespace Models.Software;

[GitFolder(FolderName = "Fields", UseNodeFolders = false)]
public record Field : Node
{
    /// <summary>Gets the description of the application.</summary>
    public string? Description { get; init; }

    public NestedA[]? A { get; init; }

    public NestedA? SomeValue { get; init; }

    public Table? LinkedTable { get; init; }
}

public record NestedA
{
    public NestedB? B { get; set; }
}

public record NestedB
{
    public bool IsVisible { get; set; }
}

public static class IConnectionFieldExtensions
{
    public static IEnumerable<Field> GetFields(this IConnection connection, Table table, string committish = "main") =>
        connection.GetNodes<Field>(committish, table);
}
