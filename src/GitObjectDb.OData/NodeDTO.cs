using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.OData
{
    public abstract class NodeDTO
    {
        public string? Id { get; set; }

        public string? Path { get; set; }

        public IEnumerable<NodeDTO> Children =>
            ChildResolver?.Invoke() ?? Enumerable.Empty<NodeDTO>();

        internal Func<IEnumerable<NodeDTO>>? ChildResolver { get; set; }
    }
}
