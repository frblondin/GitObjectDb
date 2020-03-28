using System.Collections.Generic;

namespace GitObjectDb.Model
{
    /// <summary>Semantic representation of a model.</summary>
    public interface IDataModel
    {
        /// <summary>Gets the collection of node types that are contained in this model.</summary>
        IEnumerable<NodeTypeDescription> NodeTypes { get; }
    }
}