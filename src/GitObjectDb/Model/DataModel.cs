using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Model
{
    /// <summary>Semantic representation of a model.</summary>
    internal class DataModel : IDataModel
    {
        /// <summary>Initializes a new instance of the <see cref="DataModel"/> class.</summary>
        /// <param name="nodeTypes">Collection of node types that are contained in this model.</param>
        internal DataModel(IEnumerable<NodeTypeDescription> nodeTypes)
        {
            NodeTypes = nodeTypes;
        }

        /// <inheritdoc/>
        public IEnumerable<NodeTypeDescription> NodeTypes { get; }
    }
}
