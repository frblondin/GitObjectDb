using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GitObjectDb.Tests.Assets.Models.Software
{
    [GitPath("Pages")]
    public class Table : Node
    {
        public Table(UniqueId id) : base(id)
        {
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<Field> GetFields(IConnection connection) => (this).GetChildren<Field>(connection);
    }
}
