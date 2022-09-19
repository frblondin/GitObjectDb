using GitObjectDb.Comparison;
using System.Collections.Generic;
using System.Linq;

namespace GitObjectDb.Model
{
    /// <summary>Semantic representation of a model.</summary>
    internal class DataModel : IDataModel
    {
        private readonly ILookup<string, NodeTypeDescription> _folderNames;

        internal DataModel(IEnumerable<NodeTypeDescription> nodeTypes)
        {
            NodeTypes = nodeTypes;
            DefaultComparisonPolicy = ComparisonPolicy.CreateDefault(this);
            _folderNames = NodeTypes.ToLookup(n => n.Name);
        }

        public IEnumerable<NodeTypeDescription> NodeTypes { get; }

        public ComparisonPolicy DefaultComparisonPolicy { get; }

        public IEnumerable<NodeTypeDescription> GetTypesMatchingFolderName(string folderName) =>
            _folderNames[folderName];
    }
}
