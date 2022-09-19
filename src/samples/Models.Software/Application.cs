using GitObjectDb;
using GitObjectDb.Model;
using System.Collections.Generic;

namespace Models.Software
{
    [GitFolder("Applications")]
    [HasChild(typeof(Table))]
    public record Application : Node
    {
        public string? Name { get; set; }

        public string? Description { get; set; }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public static class IConnectionApplicationExtensions
    {
        public static IEnumerable<Application> GetApplications(this IConnection connection, string? committish = null) =>
            connection.GetNodes<Application>(committish: committish);
    }
}
