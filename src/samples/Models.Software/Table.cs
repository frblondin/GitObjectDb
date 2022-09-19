using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;

namespace Models.Software
{
    [GitFolder(FolderName = "Pages")]
    public record Table : Node
    {
        public string? Name { get; set; }

        public string? Description { get; set; }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public static class IConnectionTableExtensions
    {
        public static IEnumerable<Table> GetTables(this IConnection connection, Application application, string? committish = null) =>
            connection.GetNodes<Table>(application, committish);
    }
}
