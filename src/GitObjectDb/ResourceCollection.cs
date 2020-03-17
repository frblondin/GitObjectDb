using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace GitObjectDb
{
    /// <summary>
    /// Defines a collection of <see cref="Resource"/>.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public class ResourceCollection : IReadOnlyCollection<Resource>
    {
        private readonly IDictionary<DataPath, Resource> _resources = new Dictionary<DataPath, Resource>();

        /// <summary>Initializes a new instance of the <see cref="ResourceCollection"/> class.</summary>
        /// <param name="node">The node this collection is linked to.</param>
        internal ResourceCollection(Node node)
        {
            Node = node;
        }

        /// <summary>Gets the node this collection is linked to.</summary>
        public Node Node { get; }

        /// <summary>Gets a value indicating whether this instance is detached from the repository.</summary>
        public bool IsDetached { get; internal set; } = true;

        /// <summary>Gets an <see cref="ICollection{T}"/> containing the paths of the <see cref="Resource"/>.</summary>
        /// <value>An <see cref="ICollection{T}"/> containing the paths of the <see cref="Resource"/>.</value>
        public ICollection<DataPath> Paths => _resources.Keys;

        /// <summary>Gets an <see cref="ICollection{T}"/> containing the resources of the <see cref="Resource"/>.</summary>
        /// <value>An <see cref="ICollection{T}"/> containing the paths of the <see cref="Resource"/>.</value>
        public ICollection<Resource> Resources => _resources.Values;

        /// <inheritdoc/>
        public int Count => _resources.Count;

        /// <summary>Gets or sets the resource with the specified path.</summary>
        /// <value>The <see cref="Resource"/>.</value>
        /// <param name="path">The path of the resource to get or set.</param>
        /// <returns>The resource with the specified path.</returns>
        public Resource this[DataPath path]
        {
            get => _resources[path];
        }

        /// <summary>Adds a resource with the provided path.</summary>
        /// <param name="path">The path to use.</param>
        /// <param name="data">The resource data.</param>
        /// <returns>The resource being added.</returns>
        public Resource Add(DataPath path, byte[] data) =>
            _resources[path] = new Resource(
                DataPath.FromGitBlobPath($"{Node.Path.FolderPath}/{FileSystemStorage.ResourceFolder}/{path.FilePath}"),
                data);

        internal Resource Add(DataPath path, Blob blob) =>
            _resources[path] = new Resource(
                DataPath.FromGitBlobPath($"{Node.Path.FolderPath}/{FileSystemStorage.ResourceFolder}/{path.FilePath}"),
                blob);

        /// <summary>Removes all resources.</summary>
        public void Clear() => _resources.Clear();

        /// <summary>Determines whether this instance contains a resource with the specified path.</summary>
        /// <param name="path">The path to locate.</param>
        /// <returns><c>true</c> if the instance contains a resource with the path; otherwise, <c>false</c>.</returns>
        public bool ContainsPath(DataPath path) =>
            _resources.ContainsKey(path);

        /// <summary>Removes the resource with the specified path.</summary>
        /// <param name="path">The path of the resource to remove.</param>
        /// <returns>true if the resource is successfully removed; otherwise, false.</returns>
        public bool Remove(DataPath path) =>
            _resources.Remove(path);

        /// <summary>Gets the resource associated with the specified path.</summary>
        /// <param name="path">The path whose value to get.</param>
        /// <param name="resource">When this method returns <c>true</c>, the value associated with
        /// the specified path, if the path is found; otherwise, <c>null</c>.
        /// This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the resource exists with the specified path; otherwise <c>false</c>.</returns>
        public bool TryGetResource(DataPath path, out Resource resource) =>
            _resources.TryGetValue(path, out resource);

        /// <inheritdoc/>
        public IEnumerator<Resource> GetEnumerator() =>
            _resources.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
