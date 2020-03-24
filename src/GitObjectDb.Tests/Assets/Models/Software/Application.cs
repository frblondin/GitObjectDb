using System;
using System.Collections.Generic;
using System.Linq;
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

        public IQueryable<Table> GetTables(IConnection connection) =>
            this.GetChildren<Table>(connection);
    }

    public static class IConnectionExtensions
    {
        public static IQueryable<Application> GetApplications(this IConnection connection) =>
            connection.AsQueryable().OfType<Application>();
    }
}
