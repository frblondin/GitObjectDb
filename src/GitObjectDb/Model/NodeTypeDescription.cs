using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GitObjectDb.Model
{
    /// <summary>Provides a description of a node type.</summary>
    [DebuggerDisplay("Name = {Name}, Type = {Type}")]
    public class NodeTypeDescription
    {
        private List<NodeTypeDescription> _children = new List<NodeTypeDescription>();

        internal NodeTypeDescription(Type type, string name)
        {
            Type = type;
            Name = name;

            UseNodeFolders = GitFolderAttribute.Get(type)?.UseNodeFolders ?? GitFolderAttribute.DefaultUseNodeFoldersValue;
        }

        /// <summary>Gets the CLR <see cref="Type"/> of the node.</summary>
        public Type Type { get; }

        /// <summary>Gets the name of the node type.</summary>
        public string Name { get; }

        /// <summary>Gets the children that this node can contain.</summary>
        public IEnumerable<NodeTypeDescription> Children => _children.AsReadOnly();

        /// <summary>Gets a value indicating whether node should be stored in a nested folder (FolderName/NodeId/data.json) or not (FolderName/NodeId.json).</summary>
        public bool UseNodeFolders { get; }

        internal void AddChild(NodeTypeDescription nodeType)
        {
            if (!_children.Contains(nodeType))
            {
                _children.Add(nodeType);
            }
        }
    }
}
