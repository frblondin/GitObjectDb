using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Provides a description of an object path, including the repository.
    /// </summary>
    [DataContract]
    public sealed class ObjectPath : IEquatable<ObjectPath>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPath"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="path">The path.</param>
        /// <exception cref="ArgumentNullException">path</exception>
        [JsonConstructor]
        public ObjectPath(UniqueId repository, string path)
        {
            Repository = repository;
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPath"/> class.
        /// </summary>
        /// <param name="node">The object.</param>
        public ObjectPath(IMetadataObject node)
            : this(node.Repository.Id, node.GetFolderPath())
        {
        }

        /// <summary>
        /// Gets the repository containing the object.
        /// </summary>
        [DataMember]
        public UniqueId Repository { get; }

        /// <summary>
        /// Gets the path to the object.
        /// </summary>
        [DataMember]
        public string Path { get; }

        /// <inheritdoc />
        public bool Equals(ObjectPath other) =>
            other != null &&
            other.Repository == Repository &&
            string.Equals(other.Path, Path, StringComparison.Ordinal);

        /// <inheritdoc />
        public override bool Equals(object obj) => Equals(obj as ObjectPath);

        /// <inheritdoc />
        public override int GetHashCode() => (Repository, Path).GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"{Repository}:{Path}";
    }
}
