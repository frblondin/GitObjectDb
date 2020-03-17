using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace GitObjectDb
{
    /// <summary>
    /// A set of methods for instances of <see cref="Node"/>.
    /// </summary>
    public static class NodeExtensions
    {
        /// <summary>Gets the children of the parent node.</summary>
        /// <typeparam name="TNode">The type of the node.</typeparam>
        /// <param name="parent">The parent node.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="committish">The committish.</param>
        /// <returns>The children of the parent node.</returns>
        public static IEnumerable<TNode> GetChildren<TNode>(this Node parent, IConnection connection, string committish = null)
            where TNode : Node =>
            connection.GetNodes<TNode>(parent, committish);
    }
}
