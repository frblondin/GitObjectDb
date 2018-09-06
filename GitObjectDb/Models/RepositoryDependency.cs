using System;
using System.Runtime.Serialization;

namespace GitObjectDb.Models
{
    /// <summary>
    /// Provides a description of a repository dependency.
    /// </summary>
    [DataContract]
    public class RepositoryDependency
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryDependency"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="name">The name.</param>
        /// <param name="version">The version.</param>
        /// <exception cref="ArgumentNullException">
        /// name
        /// or
        /// version
        /// </exception>
        public RepositoryDependency(Guid id, string name, Version version)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        [DataMember]
        public Guid Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        [DataMember]
        public string Name { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        [DataMember]
        public Version Version { get; }
    }
}