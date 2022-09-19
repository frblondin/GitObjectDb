using System;
using System.Collections.Generic;

namespace GitObjectDb.Model
{
    /// <summary>Semantic representation of a model.</summary>
    public interface IDataModel
    {
        /// <summary>Gets the collection of node types that are contained in this model.</summary>
        IEnumerable<NodeTypeDescription> NodeTypes { get; }

        /// <summary>Gets the types that a given <paramref name="folderName"/> should contain.</summary>
        /// <param name="folderName">The name of the folder.</param>
        /// <returns>The matching types.</returns>
        IEnumerable<NodeTypeDescription> GetTypesMatchingFolderName(string folderName);
    }
}