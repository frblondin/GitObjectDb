using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

namespace GitObjectDb.Tests.Assets.Models.Software
{
    [GitPath("Applications")]
    public class Application : Node
    {
        public Application(UniqueId id)
            : base(id)
        {
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<Table> GetTables(IConnection connection) => this.GetChildren<Table>(connection);
    }

    public static class IConnectionExtensions
    {
        public static IEnumerable<Application> GetApplications(this IConnection connection) => connection.GetNodes<Application>();
    }
}
