using GitObjectDb.Attributes;
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
#pragma warning disable CA1036 // Override methods on comparable types
    public sealed class ObjectPath : IEquatable<ObjectPath>, IComparable<ObjectPath>, IComparable
#pragma warning restore CA1036 // Override methods on comparable types
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectPath"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="path">The path.</param>
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
        public ObjectPath(IModelObject node)
            : this(node?.Repository?.Id ?? throw new ArgumentNullException(nameof(node)), node.GetFolderPath())
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

        /// <summary>
        /// Gets the full path, including the repository id.
        /// </summary>
        public string FullPath => $"{Repository}:{Path}";

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
        public override string ToString() => FullPath;

        /// <inheritdoc />
        [ExcludeFromGuardForNull]
        public int CompareTo(ObjectPath other) => string.CompareOrdinal(FullPath, other?.FullPath);

        /// <inheritdoc />
        [ExcludeFromGuardForNull]
        public int CompareTo(object obj) => CompareTo(obj as ObjectPath);
    }
}
